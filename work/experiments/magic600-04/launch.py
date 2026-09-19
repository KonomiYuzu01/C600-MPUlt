"""Owned, isolated launch for the two separately reviewed experiments."""
from pathlib import Path
import argparse
import json
import sys
import time
import webbrowser

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT))
from engine_process import EngineProcess


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--mode', choices=['g1', 'g2'], required=True)
    parser.add_argument('--session', default='review')
    parser.add_argument('--no-browser', action='store_true')
    args = parser.parse_args()
    if not args.session.replace('-', '').replace('_', '').isalnum():
        parser.error('Session name must contain letters, numbers, hyphens or underscores')
    data = HERE / 'sessions' / (args.mode + '-' + args.session)
    with EngineProcess(ROOT, data, data / 'launch.json', data / 'engine.log',
                       engine_command=[sys.executable, '-B', str(HERE / 'engine.py')],
                       hidden_console=True, progress=lambda x: print(x, flush=True)) as owned:
        url = owned.info['url'] + '&mode=' + args.mode
        (data / 'open.html').write_text('<meta http-equiv="refresh" content="0;url=' + url + '">', encoding='utf-8')
        print('Magic 600 Cell ' + args.mode.upper() + ' experiment ready. Close the app using Session > Stop experiment.', flush=True)
        if not args.no_browser:
            webbrowser.open(url)
        try:
            while owned.process.poll() is None:
                time.sleep(.5)
        except KeyboardInterrupt:
            pass


if __name__ == '__main__':
    main()
