import numpy as np
import mne

from scipy.signal import welch
from fastapi import Query

from pathlib import Path

import joblib
import pandas as pd

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

BASE_DIR = Path(__file__).resolve().parent
MODEL_PATH = BASE_DIR / "eeg_model.joblib"
FEATURES_PATH = BASE_DIR / "eeg_features.csv"

bundle = joblib.load(MODEL_PATH)
model = bundle["model"]
feature_columns = bundle["feature_columns"]
classes = bundle["classes"]

DATASET_DIR = BASE_DIR / "dataset"
DERIVATIVES_DIR = DATASET_DIR / "derivatives"

BANDS = {
    "delta": (1, 4),
    "theta": (4, 8),
    "alpha": (8, 13),
    "beta": (13, 30),
    "gamma": (30, 45),
}
# Готовые признаки для демонстрации
features_df = pd.read_csv(FEATURES_PATH)

app = FastAPI(title="EEG Classification API")


class PredictionRequest(BaseModel):
    sample_index: int = 0

def extract_features(filepath):
    raw = mne.io.read_raw_eeglab(
        str(filepath),
        preload=True,
        verbose="ERROR",
    )

    raw.pick("eeg")

    data = raw.get_data()
    sfreq = raw.info["sfreq"]

    if data.shape[1] < int(sfreq * 4):
        raise ValueError("EEG-запись короче 4 секунд")

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

            power = np.mean(psd[channel_idx, mask])

            features[f"{channel_name}_{band_name}"] = np.log10(
                max(power, 1e-30)
            )

    return features, raw

@app.get("/health")
def health():
    return {
        "status": "ok",
        "model": "RandomForest",
        "feature_count": len(feature_columns),
        "samples_available": len(features_df),
    }


@app.post("/predict")
def predict(request: PredictionRequest):
    if request.sample_index < 0 or request.sample_index >= len(features_df):
        raise HTTPException(
            status_code=400,
            detail="sample_index вне диапазона",
        )

    row = features_df.iloc[[request.sample_index]]

    try:
        # Важно: сохраняем порядок признаков обучения
        X = row[feature_columns]

        prediction = int(model.predict(X)[0])
        probabilities = model.predict_proba(X)[0]

        return {
            "sample_index": request.sample_index,
            "prediction": prediction,
            "class_name": classes[prediction],
            "probabilities": {
                str(classes[int(label)]): float(probability)
                for label, probability in zip(
                    model.classes_,
                    probabilities,
                )
            },
            "feature_count": len(feature_columns),
            "source": "precomputed_eeg_features",
        }

    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc))

@app.get("/predict_file")
def predict_file(
    participant_id: str = Query(
        ...,
        pattern=r"^sub-[A-Za-z0-9]+$",
    )
):
    # Ищем запись участника в derivatives
    matches = list(
        DERIVATIVES_DIR.rglob(
            f"{participant_id}_task-eyesclosed_eeg.set"
        )
    )

    if not matches:
        raise HTTPException(
            status_code=404,
            detail=f"EEG-файл не найден для {participant_id}",
        )

    try:
        features, raw = extract_features(matches[0])

        # Берём первый EEG-канал
        signal_data = raw.get_data(picks=[0])[0]

        # Оставляем 600 точек, чтобы график быстро отрисовывался в Unity
        point_count = min(600, len(signal_data))
        indices = np.linspace(
            0,
            len(signal_data) - 1,
            point_count,
            dtype=int
        )
        signal_data = signal_data[indices]

        # Нормализуем сигнал только для отображения на графике
        max_value = np.max(np.abs(signal_data))
        if max_value > 0:
            signal_data = signal_data / max_value

        channel_name = raw.ch_names[0]

        # Восстанавливаем признаки в том же порядке,
        # который использовался при обучении модели.
        X = pd.DataFrame([features]).reindex(
            columns=feature_columns
        )

        if X.isna().any().any():
            missing = X.columns[X.isna().any()].tolist()

            raise ValueError(
                f"Не удалось вычислить признаки: {missing}"
            )

        prediction = int(model.predict(X)[0])
        probabilities = model.predict_proba(X)[0]

        return {
            "participant_id": participant_id,
            "prediction": prediction,
            "class_name": classes[prediction],
            "probabilities": {
                str(classes[int(label)]): float(probability)
                for label, probability in zip(
                    model.classes_,
                    probabilities,
                )
            },
            "feature_count": len(feature_columns),
            "channel_count": len(raw.ch_names),
            "duration_seconds": float(raw.times[-1]),
            "source": "openneuro_set_file",
            "channel_name": channel_name,
            "signal": signal_data.tolist()
        }

    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(
            status_code=500,
            detail=str(exc),
        )