# Derived from
# https://gist.github.com/peterspackman/8cf73f7f12ba270aa8192d6911972fe8
# 
# Sample toolchain file for building for Windows from an Ubuntu Linux system.
#
# Typical usage:
#    *) install cross compiler: `sudo apt-get install mingw-w64`
#    *) cd build
#    *) cmake -DCMAKE_TOOLCHAIN_FILE=~/mingw-w64-x86_64.cmake ..
# This is free and unencumbered software released into the public domain.

# cimgui
#  git clone --recursive https://github.com/cimgui/cimgui.git
#  cd cimgui
#  mkdir build; cd build
#  cmake -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=~/mingw-w64-x86_64.cmake ..
#  cmake --build .

set(CMAKE_SYSTEM_NAME Windows)
set(TOOLCHAIN_PREFIX x86_64-w64-mingw32)  # 64bit
# set(TOOLCHAIN_PREFIX i686-w64-mingw32)  # 32bit

#set(TOOLCHAIN_SUFFIX "-win32")

# cross compilers to use for C, C++ and Fortran
set(CMAKE_C_COMPILER ${TOOLCHAIN_PREFIX}-gcc${TOOLCHAIN_SUFFIX})
set(CMAKE_CXX_COMPILER ${TOOLCHAIN_PREFIX}-g++${TOOLCHAIN_SUFFIX})
set(CMAKE_Fortran_COMPILER ${TOOLCHAIN_PREFIX}-gfortran${TOOLCHAIN_SUFFIX})
set(CMAKE_RC_COMPILER ${TOOLCHAIN_PREFIX}-windres${TOOLCHAIN_SUFFIX})

# target environment on the build host system
set(CMAKE_FIND_ROOT_PATH /usr/${TOOLCHAIN_PREFIX})

# modify default behavior of FIND_XXX() commands
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)

#set(CMAKE_CXX_STANDARD_LIBRARIES "-static-libgcc -static-libstdc++ -lwsock32 -lws2_32 ${CMAKE_CXX_STANDARD_LIBRARIES}")
#set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -Wl,-Bstatic,--whole-archive -lwinpthread -Wl,--no-whole-archive")
#set(CMAKE_C_FLAGS "-static ${CMAKE_C_FLAGS}")

# otherwise we get nasty dependencies
set(CMAKE_SHARED_LINKER_FLAGS "-static-libgcc -static-libstdc++")

