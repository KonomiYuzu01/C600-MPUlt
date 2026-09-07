"""One 0.2 engine per session directory. Kernel locks release after a hard exit."""
from pathlib import Path
import os
class SessionLock:
 def __init__(self,directory):
  self.file=(Path(directory)/'engine.lock').open('a+b');self.file.seek(0);self.file.write(b'0');self.file.flush();self.file.seek(0)
  try:
   if os.name=='nt':
    import msvcrt;msvcrt.locking(self.file.fileno(),msvcrt.LK_NBLCK,1)
   else:
    import fcntl;fcntl.flock(self.file.fileno(),fcntl.LOCK_EX|fcntl.LOCK_NB)
  except (OSError,IOError):
   self.file.close();raise RuntimeError('This session is already open in another C600 Studio 0.2 engine. Close it first or choose a different --data directory.')
 def close(self):
  if self.file.closed:return
  self.file.seek(0)
  if os.name=='nt':
   import msvcrt;msvcrt.locking(self.file.fileno(),msvcrt.LK_UNLCK,1)
  else:
   import fcntl;fcntl.flock(self.file.fileno(),fcntl.LOCK_UN)
  self.file.close()
