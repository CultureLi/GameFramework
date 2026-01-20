#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import sys
import subprocess
import shutil
import threading
import time
import json
from pathlib import Path
from typing import Optional, Callable, List

class LogMonitor:
    """日志监控器，负责监控 Unity 日志文件"""
    
    def __init__(self, log_file: Path):
        self.log_file = log_file
        self.stop_event = threading.Event()
        self.last_position = 0
        self.thread = None
        self.lines_read = 0
        
    def start(self):
        """启动日志监控"""
        print(f"开始监控日志文件: {self.log_file}")
        self.thread = threading.Thread(target=self._monitor)
        self.thread.daemon = True
        self.thread.start()
        
    def stop(self):
        """停止日志监控"""
        self.stop_event.set()
        if self.thread:
            self.thread.join(timeout=10)
            
    def _monitor(self):
        """监控日志文件"""
        # 等待文件创建
        max_wait_time = 90  # Unity 启动可能需要较长时间
        wait_count = 0
        
        while not self.stop_event.is_set() and wait_count < max_wait_time:
            if self.log_file.exists() and self.log_file.stat().st_size > 0:
                print(f"日志文件已创建且有内容，大小: {self.log_file.stat().st_size} 字节")
                break
            time.sleep(2)
            wait_count += 1
            if wait_count % 10 == 0:
                print(f"等待日志文件创建... ({wait_count*2} 秒)")
        else:
            if not self.log_file.exists():
                print(f"警告: 日志文件未在 {max_wait_time*2} 秒内创建")
            else:
                print(f"警告: 日志文件存在但为空，大小: {self.log_file.stat().st_size} 字节")
            return
        
        try:
            # 打开文件并监控
            with open(self.log_file, 'r', encoding='utf-8', errors='replace') as f:
                # 先读取所有已有内容
                initial_content = f.read()
                if initial_content:
                    print("读取初始日志内容:")
                    print("-" * 60)
                    for line in initial_content.split('\n'):
                        if line.strip():
                            print(line)
                            self.lines_read += 1
                    print("-" * 60)
                
                # 记录当前位置
                self.last_position = f.tell()
                
                # 开始实时监控
                print("开始实时监控日志...")
                while not self.stop_event.is_set():
                    # 检查文件大小
                    current_size = f.tell()
                    
                    if current_size < self.last_position:
                        # 文件被截断或重新创建，重新打开
                        print("检测到文件被重新创建，重新打开...")
                        f.close()
                        f = open(self.log_file, 'r', encoding='utf-8', errors='replace')
                        self.last_position = 0
                    
                    # 读取新内容
                    f.seek(self.last_position)
                    new_content = f.read()
                    
                    if new_content:
                        # 输出新内容
                        for line in new_content.split('\n'):
                            line = line.rstrip()
                            if line:
                                print(line)
                                self.lines_read += 1
                        
                        # 更新位置
                        self.last_position = f.tell()
                    
                    # 短暂等待
                    time.sleep(0.5)
                    
        except Exception as e:
            print(f"监控日志文件时出错: {e}")
            
    def get_lines_count(self):
        """获取读取的行数"""
        return self.lines_read

def run_unity_command(command: str) -> int:
    """
    执行 Unity 命令并实时输出
    """
    print(f"执行 Unity 命令: {command}")
    
    try:
        # 创建子进程
        process = subprocess.Popen(
            command,
            shell=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            encoding='utf-8',
            errors='replace',
            bufsize=1,
            universal_newlines=True
        )
        
        # 读取输出的函数
        def read_stream(pipe, is_stderr=False):
            try:
                for line in iter(pipe.readline, ''):
                    line = line.rstrip()
                    if line:
                        # Unity 的标准输出通常包含编译信息
                        # 但很多 Unity 日志会直接写到文件，这里我们还是输出
                        print(line)
            except Exception as e:
                if not is_stderr:
                    print(f"读取输出时出错: {e}")
        
        # 创建读取线程
        stdout_thread = threading.Thread(target=read_stream, args=(process.stdout, False))
        stderr_thread = threading.Thread(target=read_stream, args=(process.stderr, True))
        
        stdout_thread.daemon = True
        stderr_thread.daemon = True
        
        stdout_thread.start()
        stderr_thread.start()
        
        # 等待进程完成
        return_code = process.wait()
        
        # 等待线程完成
        stdout_thread.join(timeout=5)
        stderr_thread.join(timeout=5)
        
        return return_code
        
    except Exception as e:
        print(f"执行 Unity 命令时出错: {e}")
        return 1

def run_git_command(command: str) -> bool:
    """执行 Git 命令"""
    print(f"执行命令: {command}")
    
    try:
        result = subprocess.run(
            command,
            shell=True,
            capture_output=True,
            text=True,
            encoding='utf-8',
            errors='replace'
        )
        
        if result.stdout:
            for line in result.stdout.split('\n'):
                if line.strip():
                    print(line)
                    
        if result.stderr:
            for line in result.stderr.split('\n'):
                if line.strip():
                    print(f"[Git错误] {line}")
        
        return result.returncode == 0
        
    except Exception as e:
        print(f"执行命令时出错: {e}")
        return False

def clean_and_clone_project(project_dir: str, git_url: str) -> bool:
    """清理并克隆项目"""
    print(f"[清理并克隆] 清理项目目录")
    
    # 删除旧目录
    if os.path.exists(project_dir):
        print(f"删除旧文件夹: {project_dir}")
        try:
            shutil.rmtree(project_dir)
        except Exception as e:
            print(f"删除目录失败: {e}")
            return False
    
    # 克隆仓库
    print("克隆仓库...")
    return run_git_command(f'git clone "{git_url}" "{project_dir}"')

def update_existing_repository(project_dir: str) -> bool:
    """更新现有仓库"""
    print(f"[拉取更新] 更新现有仓库: {project_dir}")
    
    commands = [
        f'git -C "{project_dir}" reset --hard',
        f'git -C "{project_dir}" clean -fd',
        f'git -C "{project_dir}" pull'
    ]
    
    for cmd in commands:
        if not run_git_command(cmd):
            return False
    
    return True

def clone_if_not_exists(project_dir: str, git_url: str) -> bool:
    """如果不存在则克隆"""
    print("[克隆] 仓库不存在，正在克隆...")
    return run_git_command(f'git clone "{git_url}" "{project_dir}"')

def handle_project_repository(project_dir: str, git_url: str, clean_project: str) -> bool:
    """处理项目仓库"""
    if clean_project and clean_project.lower() == "true":
        return clean_and_clone_project(project_dir, git_url)
    else:
        if os.path.exists(project_dir):
            return update_existing_repository(project_dir)
        else:
            return clone_if_not_exists(project_dir, git_url)

def get_output_base_dir():
    """
    获取输出基础目录
    优先使用 Jenkins 工作空间，否则使用默认目录
    """
    # 获取 Jenkins 工作空间
    jenkins_workspace = os.environ.get('WORKSPACE')
    
    if jenkins_workspace and os.path.exists(jenkins_workspace):
        print(f"检测到 Jenkins 工作空间: {jenkins_workspace}")
        # 在 Jenkins 工作空间中创建输出目录
        output_base = Path(jenkins_workspace) / "BuildOutput"
    else:
        # 非 Jenkins 环境，使用相对路径
        output_base = Path.cwd() / "BuildOutput"
    
    return output_base

def setup_output_directories(version: str, platform: str):
    """
    设置输出目录结构
    """
    # 获取基础目录
    output_base = get_output_base_dir()
    
    # 创建版本化输出目录
    version_dir = output_base / version
    version_dir.mkdir(parents=True, exist_ok=True)
    
    platform_dir = version_dir / platform
    platform_dir.mkdir(parents=True, exist_ok=True)
    
    # 创建日志目录
    logs_dir = platform_dir / "Logs"
    logs_dir.mkdir(exist_ok=True)
    
    print(f"输出目录结构已创建:")
    print(f"  - 版本目录: {version_dir}")
    print(f"  - 平台目录: {platform_dir}")
    print(f"  - 日志目录: {logs_dir}")
    
    return version_dir, platform_dir, logs_dir

def create_build_manifest(version: str, output_dir: Path, target_platform: str, exit_code: int):
    """
    创建构建清单文件
    """
    manifest_file = output_dir / "build_manifest.json"
    
    manifest_data = {
        "version": version,
        "build_time": time.strftime("%Y-%m-%d %H:%M:%S"),
        "target_platform": target_platform,
        "exit_code": exit_code,
        "unity_version": "2022.3.62f2",
        "output_directory": str(output_dir),
        "artifacts": []
    }
    
    # 列出所有产物文件
    for root, dirs, files in os.walk(output_dir):
        for file in files:
            file_path = Path(root) / file
            try:
                relative_path = file_path.relative_to(output_dir)
                manifest_data["artifacts"].append({
                    "name": file,
                    "path": str(relative_path),
                    "size": file_path.stat().st_size,
                    "modified": time.strftime("%Y-%m-%d %H:%M:%S", 
                                            time.localtime(file_path.stat().st_mtime))
                })
            except ValueError:
                continue
    
    with open(manifest_file, 'w', encoding='utf-8') as f:
        json.dump(manifest_data, f, indent=2, ensure_ascii=False)
    
    print(f"构建清单已创建: {manifest_file}")
    return manifest_file

def build_unity_project(project_dir: str, version: str, target_platform: str) -> int:
    """执行 Unity 构建并实时输出日志"""
    print("\n" + "="*60)
    print("[构建 Unity 项目]")
    print("="*60)
    
    # 设置输出目录
    version_dir, platform_dir, logs_dir = setup_output_directories(version, target_platform)
    
    # 构建日志文件路径
    log_file = logs_dir / "unity_build.log"
    
    # Unity 可执行文件路径
    unity_exe = "C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe"
    
    # 项目路径
    project_path = Path(project_dir) / "Client"
    
    # 构建命令
    command_parts = [
        f'"{unity_exe}"',
        '-batchmode',
        '-nographics',
        f'-projectPath "{project_path}"',
        '-executeMethod "Assets.Editor.Build.BuildTools.BuildByCommandLine"',
        '-quit',
        f'-logFile "{log_file}"',
        f'-version="{version}"',
        f'-targetPlatform="{target_platform}"',
        f'-outputPath="{platform_dir}"'
    ]
    
    full_command = " ".join(command_parts)
    print(f"准备执行 Unity 构建")
    print(f"项目路径: {project_path}")
    print(f"日志文件: {log_file}")
    print(f"输出路径: {platform_dir}")
    
    # 创建日志监控器
    log_monitor = LogMonitor(log_file)
    
    try:
        # 启动日志监控（在 Unity 启动之前）
        print("\n启动日志监控...")
        log_monitor.start()
        
        # 给监控器一点时间开始
        time.sleep(3)
        
        print("\n" + "="*60)
        print("开始执行 Unity 构建")
        print("="*60 + "\n")
        
        # 执行 Unity 命令
        print("Unity 构建输出:")
        print("-" * 60)
        
        return_code = run_unity_command(full_command)
        
        print("-" * 60)
        
    finally:
        # 停止日志监控
        print("\n停止日志监控...")
        log_monitor.stop()
        print(f"日志监控器读取了 {log_monitor.get_lines_count()} 行日志")
    
    # 构建完成后，创建清单文件
    create_build_manifest(version, platform_dir, target_platform, return_code)
    
    # 打印归档提示
    print_archive_instructions(platform_dir, version, target_platform, return_code)
    
    return return_code

def print_archive_instructions(output_dir: Path, version: str, target_platform: str, exit_code: int):
    """
    打印 Jenkins 归档配置说明
    """
    # 获取相对于工作空间的路径
    workspace = os.environ.get('WORKSPACE', '')
    if workspace and str(output_dir).startswith(workspace):
        relative_path = Path(output_dir).relative_to(workspace)
    else:
        relative_path = Path("BuildOutput") / version / target_platform
    
    print("\n" + "="*70)
    print("构建完成总结")
    print("="*70)
    print(f"构建结果: {'成功' if exit_code == 0 else '失败'}")
    print(f"退出代码: {exit_code}")
    print(f"构建版本: {version}")
    print(f"目标平台: {target_platform}")
    print(f"输出目录: {output_dir}")
    print(f"相对路径: {relative_path}")
    print()
    
    # 检查生成的产物文件
    print("生成的文件列表:")
    print("-"*40)
    file_count = 0
    for root, dirs, files in os.walk(output_dir):
        level = root.replace(str(output_dir), '').count(os.sep)
        indent = ' ' * 2 * level
        print(f"{indent}{os.path.basename(root)}/")
        subindent = ' ' * 2 * (level + 1)
        for file in files:
            file_path = Path(root) / file
            file_size = file_path.stat().st_size
            print(f"{subindent}{file} ({file_size} bytes)")
            file_count += 1
    
    if file_count == 0:
        print("  未找到任何文件！")
    
    print()
    print("Jenkins 归档配置（复制到 'Archive the artifacts' 中）:")
    print("-"*40)
    
    # 生成归档配置
    archive_patterns = [
        f"{relative_path}/**/*.log",  # 所有日志文件
        f"{relative_path}/**/*.apk",  # Android APK
        f"{relative_path}/**/*.ipa",  # iOS IPA
        f"{relative_path}/**/*.exe",  # Windows EXE
        f"{relative_path}/**/*.aab",  # Android App Bundle
        f"{relative_path}/build_manifest.json"  # 构建清单
    ]
    
    # 打印归档配置
    for pattern in archive_patterns:
        print(pattern)
    
    # 单行配置版本
    print("\n单行配置版本:")
    print("-"*40)
    single_line = ",".join(archive_patterns)
    print(single_line)
    
    # 简单版本（推荐）
    print("\n推荐配置:")
    print("-"*40)
    print(f"{relative_path}/**/*")
    
    print("="*70)

def main():
    """主函数"""
    # 检查参数数量
    if len(sys.argv) < 3:
        print("用法: python build_unity.py <Version> <TargetPlatform> [CleanProject]")
        print("参数说明:")
        print("  Version: 构建版本号，如 1.0.0")
        print("  TargetPlatform: 目标平台，如 Android, iOS, Windows")
        print("  CleanProject: 可选，是否清理项目，默认为 false")
        print()
        print("示例:")
        print("  python build_unity.py 1.0.0 Android")
        print("  python build_unity.py 2.1.0 Windows true")
        sys.exit(1)
    
    # 解析参数
    version = sys.argv[1]
    target_platform = sys.argv[2]
    clean_project = sys.argv[3] if len(sys.argv) > 3 else "false"
    
    print("="*50)
    print("Unity 自动化构建脚本")
    print("="*50)
    print(f"构建参数:")
    print(f"  - 版本: {version}")
    print(f"  - 目标平台: {target_platform}")
    print(f"  - 清理项目: {clean_project}")
    print("="*50)
    
    # 环境变量
    project_dir = r"E:\Work\BuildDir\AutoBuild"
    git_url = "https://github.com/CultureLi/GameFramework.git"
    
    # 处理项目仓库
    print("\n" + "="*40)
    print("处理工程目录")
    print("="*40)
    
    if not handle_project_repository(project_dir, git_url, clean_project):
        print("处理项目仓库失败!")
        sys.exit(1)
    
    # 执行 Unity 构建
    exit_code = build_unity_project(project_dir, version, target_platform)
    
    print("\n" + "="*40)
    print(f"构建完成，退出代码: {exit_code}")
    print("="*40)
    
    sys.exit(exit_code)

if __name__ == "__main__":
    main()