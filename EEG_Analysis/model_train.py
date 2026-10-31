from pathlib import Path
import warnings

import numpy as np
import pandas as pd
import mne

from scipy.signal import welch
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import (
    train_test_split,
    StratifiedKFold,
    cross_val_score,
)
from sklearn.metrics import (
    accuracy_score,
    balanced_accuracy_score,
    classification_report,
    confusion_matrix,
)
from joblib import dump

warnings.filterwarnings("ignore", category=RuntimeWarning)
mne.set_log_level("ERROR")

# УКАЖИ ПУТЬ К КОРНЕВОЙ ПАПКЕ ДАТАСЕТА
DATASET_DIR = Path(r"C:\Users\faral\Downloads\v1.0.0")

PARTICIPANTS_FILE = DATASET_DIR / "participants.tsv"
DERIVATIVES_DIR = DATASET_DIR / "derivatives"

# Частотные диапазоны ЭЭГ, Гц
BANDS = {
    "delta": (1, 4),
    "theta": (4, 8),
    "alpha": (8, 13),
    "beta": (13, 30),
    "gamma": (30, 45),
}


def extract_features(filepath):
    """Извлекает спектральные признаки из одной EEG-записи."""

    raw = mne.io.read_raw_eeglab(
        str(filepath),
        preload=True,
        verbose="ERROR",
    )

    # Берём только EEG-каналы
    raw.pick("eeg")

    data = raw.get_data()
    sfreq = raw.info["sfreq"]

    if data.shape[1] < int(sfreq * 4):
        raise ValueError("Запись слишком короткая")

    # Спектральная плотность мощности
    freqs, psd = welch(
        data,
        fs=sfreq,
        nperseg=int(sfreq * 4),
        axis=1,
    )

    features = {}

    for channel_idx, channel_name in enumerate(raw.ch_names):
        for band_name, (low, high) in BANDS.items():
            mask = (freqs >= low) & (freqs < high)

            if not np.any(mask):
                raise ValueError(
                    f"Нет частот для диапазона {band_name}"
                )

            # Логарифм мощности стабилизирует масштаб признаков
            power = np.mean(psd[channel_idx, mask])
            features[f"{channel_name}_{band_name}"] = np.log10(
                max(power, 1e-30)
            )

    return features


def main():
    participants = pd.read_csv(
        PARTICIPANTS_FILE,
        sep="\t",
    )

    print("Распределение участников:")
    print(participants["Group"].value_counts())

    # Оставляем только пациентов с AD и здоровую контрольную группу
    participants = participants[
        participants["Group"].isin(["A", "C"])
    ].copy()

    X_rows = []
    y_rows = []
    participant_ids = []

    for _, participant in participants.iterrows():
        pid = participant["participant_id"]

        # Ищем предварительно обработанную запись
        matches = list(
            DERIVATIVES_DIR.rglob(
                f"{pid}_task-eyesclosed_eeg.set"
            )
        )

        if not matches:
            print(f"[ПРОПУСК] Не найден файл derivatives: {pid}")
            continue

        try:
            features = extract_features(matches[0])

            X_rows.append(features)
            y_rows.append(
                1 if participant["Group"] == "A" else 0
            )
            participant_ids.append(pid)

            print(f"[OK] {pid}: группа {participant['Group']}")

        except Exception as exc:
            print(f"[ОШИБКА] {pid}: {exc}")

    if not X_rows:
        raise RuntimeError(
            "Не удалось обработать ни одной записи. "
            "Проверь путь и структуру derivatives."
        )

    X = pd.DataFrame(X_rows).replace(
        [np.inf, -np.inf], np.nan
    )

    # Удаляем признаки, которые не удалось вычислить
    X = X.dropna(axis=1)

    y = np.asarray(y_rows)

    print("\nРазмер матрицы признаков:", X.shape)
    print("Участников в выборке:", len(y))
    print("AD:", int(np.sum(y == 1)))
    print("Контрольная группа:", int(np.sum(y == 0)))

    if len(np.unique(y)) != 2 or np.min(np.bincount(y)) < 2:
        raise RuntimeError(
            "Недостаточно участников одной из групп для обучения."
        )

    # Каждый участник представлен одной строкой:
    # окна одного человека не попадают одновременно в train и test.
    X_train, X_test, y_train, y_test = train_test_split(
        X,
        y,
        test_size=0.25,
        random_state=42,
        stratify=y,
    )

    model = RandomForestClassifier(
        n_estimators=300,
        class_weight="balanced",
        min_samples_leaf=2,
        random_state=42,
        n_jobs=-1,
    )

    model.fit(X_train, y_train)

    predictions = model.predict(X_test)

    print("\n=== Результаты на отложенной выборке ===")
    print("Accuracy:", round(accuracy_score(y_test, predictions), 3))
    print(
        "Balanced accuracy:",
        round(balanced_accuracy_score(y_test, predictions), 3),
    )

    print("\nОтчёт классификации:")
    print(
        classification_report(
            y_test,
            predictions,
            labels=[0, 1],
            target_names=["CN (контроль)", "AD (Альцгеймер)"],
            zero_division=0,
        )
    )

    print("Матрица ошибок:")
    print(confusion_matrix(y_test, predictions, labels=[0, 1]))

    # Дополнительная проверка с разделением по участникам.
    # Это оценка на небольшой выборке, а не клиническая валидация.
    min_class_count = np.bincount(y).min()
    n_splits = min(5, int(min_class_count))

    if n_splits >= 2:
        cv = StratifiedKFold(
            n_splits=n_splits,
            shuffle=True,
            random_state=42,
        )

        scores = cross_val_score(
            model,
            X,
            y,
            cv=cv,
            scoring="balanced_accuracy",
            n_jobs=1,
        )

        print("\n=== Кросс-валидация ===")
        print("Balanced accuracy по фолдам:", np.round(scores, 3))
        print("Среднее:", round(scores.mean(), 3))
        print("Стандартное отклонение:", round(scores.std(), 3))

    # Обучаем итоговую модель на всех доступных участниках
    model.fit(X, y)

    output_dir = DATASET_DIR / "model_output"
    output_dir.mkdir(exist_ok=True)

    dump(
        {
            "model": model,
            "feature_columns": X.columns.tolist(),
            "bands": BANDS,
            "classes": {0: "CN", 1: "AD"},
        },
        output_dir / "eeg_model.joblib",
    )

    X.assign(
        participant_id=participant_ids,
        label=y,
    ).to_csv(
        output_dir / "eeg_features.csv",
        index=False,
    )

    print("\nМодель сохранена:")
    print(output_dir / "eeg_model.joblib")
    print("Признаки сохранены:")
    print(output_dir / "eeg_features.csv")


if __name__ == "__main__":
    main()