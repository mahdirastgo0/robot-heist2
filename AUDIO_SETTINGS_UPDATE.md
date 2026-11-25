# Robot Heist — Audio & Arsenal

Based on City Rebuild. Open `unity` in Unity 2022.1.24f1. Back up existing saves/project before updating. The new version uses the existing native plugin and save system. No new dependencies or downloads are required.

## Settings
Main menu > Settings, or Esc > Settings in the field/base. Audio: master, music, effects and vehicle engines. Controls: mouse sensitivity, invert Y, 23 remappable actions. Click an action then press a keyboard key or use a Mouse 1/2/3 button. Esc remains reserved; F8/F9 remain developer shortcuts. Duplicate keys are rejected except jump/brake. Display: FOV, camera recoil effect, low/medium/high graphics. Save & back persists settings in PlayerPrefs. Reset defaults also saves. Key hints use the current bindings. The camera recoil toggle affects presentation, not gun damage. The settings do not change the real-time native 72-hour server cycle.

## Imported art
Your Gun, MP5 and Shotgun OBJ meshes are normalized and triangulated for in-game use. Pistol uses supplied Gun.png. MP5 referenced missing external textures and the solid shotgun RAR yielded the OBJ but not its texture maps with the available extractor; both use replacement materials. The original three models are cosmetic game assets with hand/muzzle markers. Rifle and LMG retain their previous models. Verify the uploaded assets' distribution rights before publishing.

## Audio
- Unnamed WAV/LPCM clips (both actually PCM WAVE) interpreted as two vehicle engine layers, normalized, mono, looped and blended by vehicle speed. Their original source identity is not confirmed.
- Uploaded LMG shot used for LMG gunfire, with pitch variation in existing playback. Other weapon sounds retained.
- Uploaded police siren loops while wanted in the city, scaled by wanted level.
- Two new original instrumental electronic cues, menu/base at 88 BPM and city at 116 BPM. Generated offline; no commercial song or external music download.

## Checks and limits
Unity Editor is not installed in this environment, so Unity compilation, texture import, alignment, animation/hand fit, mix, and settings interaction need an in-editor test. Existing native tests and OBJ/audio file validation are independent of Unity compilation. In Play Mode test a car from idle to full speed, entering/exiting, wanted siren, all five guns, rebinding and persistence after restart, and map marker / Formula orientation from City Rebuild.
