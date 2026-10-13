# Install script for directory: E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean

# Set the install prefix
if(NOT DEFINED CMAKE_INSTALL_PREFIX)
  set(CMAKE_INSTALL_PREFIX "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/pkgs/sdl2_x64-windows/debug")
endif()
string(REGEX REPLACE "/$" "" CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}")

# Set the install configuration name.
if(NOT DEFINED CMAKE_INSTALL_CONFIG_NAME)
  if(BUILD_TYPE)
    string(REGEX REPLACE "^[^A-Za-z0-9_]+" ""
           CMAKE_INSTALL_CONFIG_NAME "${BUILD_TYPE}")
  else()
    set(CMAKE_INSTALL_CONFIG_NAME "Debug")
  endif()
  message(STATUS "Install configuration: \"${CMAKE_INSTALL_CONFIG_NAME}\"")
endif()

# Set the component getting installed.
if(NOT CMAKE_INSTALL_COMPONENT)
  if(COMPONENT)
    message(STATUS "Install component: \"${COMPONENT}\"")
    set(CMAKE_INSTALL_COMPONENT "${COMPONENT}")
  else()
    set(CMAKE_INSTALL_COMPONENT)
  endif()
endif()

# Is this installation the result of a crosscompile?
if(NOT DEFINED CMAKE_CROSSCOMPILING)
  set(CMAKE_CROSSCOMPILING "OFF")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/SDL2d.lib")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/SDL2d.dll")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE FILE OPTIONAL FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/SDL2d.pdb")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/SDL2maind.lib")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE FILE OPTIONAL FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/CMakeFiles/SDL2main.dir/SDL2main.pdb")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2Targets.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2Targets.cmake"
         "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2Targets.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2Targets-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2Targets.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2Targets.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2Targets-debug.cmake")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2mainTargets.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2mainTargets.cmake"
         "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2mainTargets.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2mainTargets-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2mainTargets.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2mainTargets.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2mainTargets-debug.cmake")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Devel" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/SDL2Config.cmake"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/SDL2ConfigVersion.cmake"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/cmake/sdlfind.cmake"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/SDL2" TYPE FILE FILES
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_assert.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_atomic.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_audio.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_bits.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_blendmode.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_clipboard.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_copying.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_cpuinfo.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_egl.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_endian.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_error.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_events.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_filesystem.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_gamecontroller.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_gesture.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_guid.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_haptic.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_hidapi.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_hints.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_joystick.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_keyboard.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_keycode.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_loadso.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_locale.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_log.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_main.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_messagebox.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_metal.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_misc.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_mouse.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_mutex.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_name.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_opengl.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_opengl_glext.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_opengles.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_opengles2.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_opengles2_gl2.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_opengles2_gl2ext.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_opengles2_gl2platform.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_opengles2_khrplatform.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_pixels.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_platform.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_power.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_quit.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_rect.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_render.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_rwops.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_scancode.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_sensor.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_shape.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_stdinc.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_surface.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_system.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_syswm.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_assert.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_common.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_compare.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_crc32.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_font.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_fuzzer.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_harness.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_images.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_log.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_md5.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_memory.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_test_random.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_thread.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_timer.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_touch.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_types.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_version.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_video.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/SDL_vulkan.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/begin_code.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/include/close_code.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/include/SDL2/SDL_revision.h"
    "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/include-config-debug/SDL2/SDL_config.h"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/licenses/SDL2" TYPE FILE FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/src/se-2.32.10-3b143ac573.clean/LICENSE.txt")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/pkgconfig" TYPE FILE FILES "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/sdl2.pc")
endif()

string(REPLACE ";" "\n" CMAKE_INSTALL_MANIFEST_CONTENT
       "${CMAKE_INSTALL_MANIFEST_FILES}")
if(CMAKE_INSTALL_LOCAL_ONLY)
  file(WRITE "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/install_local_manifest.txt"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
endif()
if(CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_COMPONENT MATCHES "^[a-zA-Z0-9_.+-]+$")
    set(CMAKE_INSTALL_MANIFEST "install_manifest_${CMAKE_INSTALL_COMPONENT}.txt")
  else()
    string(MD5 CMAKE_INST_COMP_HASH "${CMAKE_INSTALL_COMPONENT}")
    set(CMAKE_INSTALL_MANIFEST "install_manifest_${CMAKE_INST_COMP_HASH}.txt")
    unset(CMAKE_INST_COMP_HASH)
  endif()
else()
  set(CMAKE_INSTALL_MANIFEST "install_manifest.txt")
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  file(WRITE "E:/Work/RecastNavigationServer/build/vcpkg_installed/vcpkg/blds/sdl2/x64-windows-dbg/${CMAKE_INSTALL_MANIFEST}"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
endif()
