cmake_minimum_required(VERSION 3.15)
project(RobotHeistCore CXX)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
add_library(RobotHeistCore SHARED core.cpp)
set_target_properties(RobotHeistCore PROPERTIES
    WINDOWS_EXPORT_ALL_SYMBOLS OFF
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../unity/Assets/Plugins/x86_64"
    RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_SOURCE_DIR}/../unity/Assets/Plugins/x86_64"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../unity/Assets/Plugins/x86_64")
