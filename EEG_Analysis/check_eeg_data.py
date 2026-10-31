import pandas as pd
import numpy as np

# Путь к CSV-файлу
from pathlib import Path

path = Path(__file__).resolve().parent / "my_eeg_csv_test_INSIGHT.csv"

# Первая строка содержит метаданные, поэтому пропускаем её
df = pd.read_csv(path, skiprows=1)

channels = ["AF3", "T7", "Pz", "T8", "AF4"]
bands = ["Theta", "Alpha", "BetaL", "BetaH", "Gamma"]

print("=" * 70)
print("1. ОБЩАЯ ИНФОРМАЦИЯ")
print("=" * 70)

print("Размер таблицы:", df.shape)
print("Названия столбцов:")
print(df.columns.tolist())

# Проверяем длительность записи
if "Timestamp" in df.columns:
    timestamps = pd.to_numeric(df["Timestamp"], errors="coerce").dropna()

    if len(timestamps) > 1:
        duration = timestamps.iloc[-1] - timestamps.iloc[0]
        print(f"\nДлительность записи: {duration:.2f} секунд")

print("\n" + "=" * 70)
print("2. СЫРЫЕ EEG-СИГНАЛЫ")
print("=" * 70)

for channel in channels:
    channel_str = "EEG."+channel
    if channel_str not in df.columns:
        print(f"\nКанал {channel_str}: столбец не найден")
        continue

    signal = pd.to_numeric(df[channel_str], errors="coerce").dropna()

    print(f"\nКанал {channel_str}:")
    print(f"  Количество значений: {len(signal)}")
    print(f"  Среднее:             {signal.mean():.3f}")
    print(f"  Стандартное отклонение: {signal.std():.3f}")
    print(f"  Минимум:             {signal.min():.3f}")
    print(f"  Максимум:            {signal.max():.3f}")
    print(f"  Медиана:             {signal.median():.3f}")
    print(f"  Пропусков:           {df[channel_str].isna().sum()}")

    # Центрирование нужно только для анализа колебаний
    centered = signal - signal.mean()

    print(f"  После вычитания среднего:")
    print(f"    Минимум:           {centered.min():.3f}")
    print(f"    Максимум:          {centered.max():.3f}")
    print(f"    Среднеквадратичное отклонение: {centered.std():.3f}")

print("\n" + "=" * 70)
print("3. МОЩНОСТЬ EEG ПО ДИАПАЗОНАМ ЧАСТОТ")
print("=" * 70)

for channel in channels:
    print(f"\n--- Канал {channel} ---")

    for band in bands:
        column = f"POW.{channel}.{band}"

        if column not in df.columns:
            print(f"{band}: столбец не найден")
            continue

        values = pd.to_numeric(df[column], errors="coerce").dropna()

        if len(values) == 0:
            print(f"{band}: нет значений")
            continue

        print(
            f"{band:6s} | "
            f"N={len(values):3d} | "
            f"среднее={values.mean():9.4f} | "
            f"медиана={values.median():9.4f} | "
            f"std={values.std():9.4f} | "
            f"min={values.min():9.4f} | "
            f"max={values.max():9.4f}"
        )

print("\n" + "=" * 70)
print("4. КОРРЕЛЯЦИИ МЕЖДУ СЫРЫМИ EEG-КАНАЛАМИ")
print("=" * 70)

available_channels = [ch for ch in channels if ch in df.columns]

eeg = df[available_channels].apply(pd.to_numeric, errors="coerce")
print(eeg.corr().round(3).to_string())

print("\n" + "=" * 70)
print("5. ПЕРВЫЕ 10 СТРОК POWER-ДАННЫХ")
print("=" * 70)

power_columns = [
    f"POW.{channel}.{band}"
    for channel in channels
    for band in bands
    if f"POW.{channel}.{band}" in df.columns
]

if power_columns:
    cols_to_show = (
        ["Timestamp"] if "Timestamp" in df.columns else []
    ) + power_columns

    print(df[cols_to_show].head(10).to_string(index=False))
else:
    print("Столбцы POW не найдены.")

print("\nАнализ завершён.")