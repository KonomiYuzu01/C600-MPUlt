"""Private console entry point: stdin belongs to EngineProcess, never a browser."""
import os
import sys

if __name__ == '__main__':
    if os.environ.get('C600_WATCH_PARENT_STDIN') != '1' or len(os.environ.get('C600_LAUNCH_ID', '')) < 32:
        raise SystemExit('Start C600Studio.exe to open the puzzle. C600Engine.exe is its private state engine.')
    import server
    server.main()
