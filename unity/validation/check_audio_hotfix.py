from pathlib import Path
import wave, struct, math, hashlib
root=Path(__file__).resolve().parents[1]; a=root/'unity/Assets/Resources/Audio'; s=root/'unity/Assets/Scripts'
files=['engine_low','engine_high']+['combat_'+n for n in ['pistol','smg','shotgun','rifle','lmg']]
hashes=set()
for name in files:
 p=a/(name+'.wav')
 with wave.open(str(p),'rb') as w:
  assert (w.getnchannels(),w.getsampwidth(),w.getframerate())==(1,2,44100)
  data=w.readframes(w.getnframes()); x=struct.unpack('<'+'h'*(len(data)//2),data)
  rms=math.sqrt(sum(v*v for v in x)/len(x))/32768
  assert .01<rms<.65 and max(map(abs,x))<32767
  if name.startswith('engine'): assert abs(x[0]-x[-1])/32768<.15
  assert (a/(name+'.wav.meta')).exists()
  hashes.add(hashlib.sha256(data).hexdigest());print(name,'RMS',round(rms,3),'seconds',round(len(x)/44100,2))
assert len(hashes)==len(files)
audio=(s/'GameAudio.cs').read_text(); pc=(s/'PlayerController.cs').read_text(); car=(s/'DriveCar.cs').read_text(); cop=(s/'RobotCop.cs').read_text()
assert 'gunSource.PlayOneShot(c' in audio and 'combatShots[wi] ??' in audio
assert pc.count('GameAudio.I.Shot(weapon)')==1 and '!Driving && !gm.InputBlocked && cc.enabled' in pc
assert 'GameAudio.I.StopSteps()' in pc and 'player.VehicleThrottle' in audio
assert 'if(cop==null)return false;' in car and 'cop.TakeDamage(impactSpeed*20f)' in car and 'col.enabled=false' in cop
print('Audio files and static routing/collision checks passed; these are not Unity runtime tests.')
