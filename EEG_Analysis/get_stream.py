import time
from pylsl import StreamInlet, resolve_streams


def get_lsl_stream():
    # 1. Look for active LSL streams on the network
    print("Looking for an LSL stream...")
    # You can also pass parameters like resolve_stream('type', 'EEG')
    streams = resolve_streams()

    if not streams:
        print("No LSL streams found.")
        return

    # 2. Select the first available stream and create an inlet
    print(f"Found stream: {streams[0].name()}")
    inlet = StreamInlet(streams[0])

    # 3. Continuously pull data samples and their corresponding timestamps
    print("Pulling data...")
    try:
        while True:
            # sample contains the data data vector, timestamp is the local clock time
            sample, timestamp = inlet.pull_sample()
            if timestamp:
                print(f"Timestamp: {timestamp} | Data: {sample}")
    except KeyboardInterrupt:
        print("\nStopping data collection.")


if __name__ == "__main__":
    get_lsl_stream()