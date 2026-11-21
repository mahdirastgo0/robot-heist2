#!/bin/sh
# لینوکس / مک
cd "$(dirname "$0")"
mkdir -p ../unity/Assets/Plugins/x86_64
case "$(uname)" in
  Darwin) g++ -std=c++17 -O2 -shared -fPIC core.cpp -o ../unity/Assets/Plugins/x86_64/RobotHeistCore.dylib ;;
  *)      g++ -std=c++17 -O2 -shared -fPIC core.cpp -o ../unity/Assets/Plugins/x86_64/libRobotHeistCore.so ;;
esac
echo done
