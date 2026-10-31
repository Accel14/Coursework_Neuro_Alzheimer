from pathlib import Path
from joblib import load

path = str(Path(__file__).resolve().parent / "eeg_model.joblib")

bundle = load(path)

print("Количество признаков:", len(bundle["feature_columns"]))
print("\nВсе признаки:")

for feature in bundle["feature_columns"]:
    print(feature)