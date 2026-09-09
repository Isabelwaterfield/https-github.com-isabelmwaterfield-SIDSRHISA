SIDSRH Android — clean Gradle fix
=================================

This project uses:
  Android Gradle Plugin 8.2.2
  Gradle 8.2
  viewBinding OFF, dataBinding OFF

DO NOT open with Gradle 9 if Android Studio forces it.
In Android Studio:
  File → Settings → Build → Gradle → Gradle JDK = 17
  Use Gradle from: 'gradle-wrapper.properties' file

Steps:
1. Unzip SIDSRH_Android_Clean
2. Android Studio → Open this folder
3. Let it download Gradle 8.2 (not 9.x)
4. File → Invalidate Caches → Restart if needed
5. Build → Rebuild Project
6. Run on emulator (API running at localhost:5080)

If Studio still uses Gradle 9:
  File → Settings → Build → Gradle → 
  "Use Gradle from" → Specified location → point to Gradle 8.2
  OR delete the project's .gradle folder and re-open

Phone: set API_BASE_URL in app/build.gradle to http://YOUR_PC_IP:5080/
