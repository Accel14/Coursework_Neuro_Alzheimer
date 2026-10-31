from pathlib import Path
import pandas as pd

DATASET_DIR = Path(__file__).resolve().parent / "dataset"

participants = pd.read_csv(
    DATASET_DIR / "participants.tsv",
    sep="\t"
)

print("Столбцы:", participants.columns.tolist())
print(participants.head(10).to_string(index=False))
print("\nРазмер:", participants.shape)