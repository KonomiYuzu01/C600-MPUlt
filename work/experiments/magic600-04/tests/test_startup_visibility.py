"""Fresh full visibility must not overwrite saved display choices."""
from pathlib import Path
import sys
import tempfile
import threading
import unittest

HERE = Path(__file__).resolve().parents[1]
sys.path[:0] = [str(HERE), str(HERE.parents[2])]
from adapter import Workbench
from core import Model
from session import Session


class StartupVisibility(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.model = Model()

    def test_fresh_session_and_workspace_show_complete_model(self):
        with tempfile.TemporaryDirectory(prefix='magic600-startup-') as directory:
            session = Session(self.model, Path(directory))
            try:
                self.assertEqual(int((session.render_styles() != 0).sum()), 259800)
                work = Workbench(session, threading.RLock())
                self.assertEqual(work.w['filter'], 'all')
                self.assertEqual(int((session.render_styles() != 0).sum()), 259800)
                self.assertEqual(session.prefs['view'], dict(native_hide_frame=True,
                    native_adaptive_motion=True))
                self.assertEqual(work.new_workspace()['filter'], 'active')
            finally:
                session.close()

    def test_saved_active_and_display_choices_survive_reopen(self):
        with tempfile.TemporaryDirectory(prefix='magic600-startup-saved-') as directory:
            session = Session(self.model, Path(directory))
            choices = dict(native_hide_frame=False, native_adaptive_motion=False)
            rules = [dict(expr='active', style='solid')]
            session.save_prefs(dict(rules=rules, view=choices))
            Workbench(session, threading.RLock())
            session.close()
            session = Session(self.model, Path(directory))
            try:
                work = Workbench(session, threading.RLock())
                self.assertEqual(session.prefs['rules'], rules)
                self.assertEqual(session.prefs['view'], choices)
                self.assertEqual(work.w['filter'], 'active')
                self.assertLess(int((session.render_styles() != 0).sum()), 259800)
            finally:
                session.close()


if __name__ == '__main__':
    unittest.main()
