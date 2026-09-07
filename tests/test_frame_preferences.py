"""Persisted native frame visibility never changes the labelled puzzle."""
from pathlib import Path
import json, sys, tempfile

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from core import Model
from session import Session


def main():
    model = Model()
    with tempfile.TemporaryDirectory(prefix='c600-frame-prefs-') as directory:
        session = Session(model, Path(directory))
        original = session.st.hash
        try:
            session.save_prefs({'view': {'native_hide_frame': False, 'zoom_note': 'preserved'}})
            session.checkpoint('Visible frame')
            session.save_prefs({'view': {'native_hide_frame': True, 'zoom_note': 'preserved'}})
            session.restore('Visible frame')
            assert session.prefs['view'] == {'native_hide_frame': False, 'zoom_note': 'preserved'}
            session.reset()
            assert session.prefs['view']['native_hide_frame'] is False
            for invalid in ('false', 0, 1, None, [], {}):
                before = json.dumps(session.prefs, sort_keys=True)
                try:
                    session.save_prefs({'view': {'native_hide_frame': invalid}})
                except ValueError:
                    pass
                else:
                    raise AssertionError('Invalid frame preference accepted')
                assert json.dumps(session.prefs, sort_keys=True) == before
            assert session.st.hash == original
        finally:
            session.close()
        reopened = Session(model, Path(directory))
        try:
            assert reopened.prefs['view']['native_hide_frame'] is False
            assert reopened.prefs['view']['zoom_note'] == 'preserved'
            assert reopened.st.hash == original
        finally:
            reopened.close()
    print('PASS: frame preference, checkpoint/reset/reopen, invalid types, other view settings and all labelled slots')


if __name__ == '__main__':
    main()
