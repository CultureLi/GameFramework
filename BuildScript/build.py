#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import sys
import subprocess
import shutil
import threading
import time
from pathlib import Path
from typing import Optional, Callable

def run_command_realtime(command: str, cwd: Optional[str] = None, 
                         stdout_callback: Optional[Callable] = None,
                         stderr_callback: Optional[Callable] = None) -> int:
    """
    实时执行命令并输出到控制台
    """
    print(f"执行命令: {command}")
    
    try:
        # 创建子进程，实时获取输出
        process = subprocess.Popen(
            command,
            cwd=cwd,
            shell=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            encoding='utf-8',
            errors='replace',  # 处理编码错误
            bufsize=1,  # 行缓冲
            universal_newlines=True
        )
        
        # 定义读取输出的函数
        def read_output(pipe, callback):
            try:
                for line in iter(pipe.readline, ''):
                    if callback:
                        callback(line.rstrip())
                    else:
                        print(line, end='', flush=True)
            except Exception as e:
                print(f"读取输出时出错: {e}")
        
        # 创建线程读取 stdout 和 stderr
        stdout_thread = threading.Thread(
            target=read_output,
            args=(process.stdout, stdout_callback)
        )
        stderr_thread = threading.Thread(
            target=read_output,
            args=(process.stderr, stderr_callback)
        )
        
        # 启动线程
        stdout_thread.daemon = True
        stderr_thread.daemon = True
        stdout_thread.start()
        stderr_thread.start()
        
        # 等待进程完成
        return_code = process.wait()
        
        # 等待输出线程完成
        stdout_thread.join(timeout=5)
        stderr_thread.join(timeout=5)
        
        return return_code
        
    except Exception as e:
        print(f"执行命令时发生异常: {e}")
        return 1

def tail_log_file(log_file: Path, stop_event: threading.Event):
    """
    实时跟踪日志文件并输出
    """
    print(f"开始跟踪日志文件: {log_file}")
    
    # 等待文件创建
    max_wait_time = 30  # 最多等待30秒
    wait_interval = 1
    
    for _ in range(max_wait_time):
        if log_file.exists():
            break
        time.sleep(wait_interval)
    else:
        print(f"警告: 日志文件未在 {max_wait_time} 秒内创建")
        return
    
    try:
        # 打开文件并实时读取
        with open(log_file, 'r', encoding='utf-8', errors='replace') as f:
            # 先读取已有内容
            f.seek(0, 2)  # 移动到文件末尾
            
            while not stop_event.is_set():
                line = f.readline()
                if line:
                    print(line.rstrip(), flush=True)
                else:
                    # 没有新内容，短暂等待
                    time.sleep(0.1)
    except FileNotFoundError:
        print(f"日志文件不存在: {log_file}")
    except Exception as e:
        print(f"读取日志文件时出错: {e}")

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
    return_code = run_command_realtime(f'git clone "{git_url}" "{project_dir}"')
    return return_code == 0

def update_existing_repository(project_dir: str) -> bool:
    """更新现有仓库"""
    print(f"[拉取更新] 更新现有仓库: {project_dir}")
    
    commands = [
        f'git -C "{project_dir}" reset --hard',
        f'git -C "{project_dir}" clean -fd',
        f'git -C "{project_dir}" pull'
    ]
    
    for cmd in commands:
        return_code = run_command_realtime(cmd)
        if return_code != 0:
            return False
    
    return True

def clone_if_not_exists(project_dir: str, git_url: str) -> bool:
    """如果不存在则克隆"""
    print("[克隆] 仓库不存在，正在克隆...")
    return_code = run_command_realtime(f'git clone "{git_url}" "{project_dir}"')
    return return_code == 0

def handle_project_repository(project_dir: str, git_url: str, clean_project: str) -> bool:
    """处理项目仓库"""
    if clean_project and clean_project.lower() == "true":
        return clean_and_clone_project(project_dir, git_url)
    else:
        if os.path.exists(project_dir):
            return update_existing_repository(project_dir)
        else:
            return clone_if_not_exists(project_dir, git_url)

def build_unity_project(project_dir: str, version: str, 
                       build_addressable: str, target_platform: str) -> int:
    """执行 Unity 构建并实时输出日志"""
    print("[构建 Unity 项目]")
    
    # 构建日志文件路径
    log_dir = Path("E:/Work/BuildDir/Output") / version
    log_dir.mkdir(parents=True, exist_ok=True)
    log_file = log_dir / "unity_build.log"
    
    # Unity 可执行文件路径
    unity_exe = "C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe"
    
    # 项目路径
    project_path = Path(project_dir) / "Client"
    
    # 构建命令
    command_parts = [
        f'"{unity_exe}"',
        '-batchmode',
        f'-projectPath "{project_path}"',
        '-executeMethod "Assets.Editor.Build.BuildTools.BuildByCommandLine"',
        '-quit',
        f'-logFile "{log_file}"',
        f'--version="{version}"',
        f'--buildAddressable="{build_addressable}"',
        f'--targetPlatform="{target_platform}"'
    ]
    
    full_command = " ".join(command_parts)
    print(f"执行 Unity 构建命令: {full_command}")
    
    # 创建事件来控制日志跟踪线程
    stop_event = threading.Event()
    
    # 启动日志跟踪线程
    log_thread = threading.Thread(
        target=tail_log_file,
        args=(log_file, stop_event)
    )
    log_thread.daemon = True
    log_thread.start()
    
    # 给 Unity 一点时间开始创建日志文件
    time.sleep(2)
    
    try:
        # 执行 Unity 构建命令
        print("\n" + "="*60)
        print("开始 Unity 构建...")
        print("="*60 + "\n")
        
        # 同时输出命令行和日志文件
        def handle_stdout(line: str):
            if line.strip():
                print(f"[Unity输出] {line}")
        
        def handle_stderr(line: str):
            if line.strip():
                print(f"[Unity错误] {line}", file=sys.stderr)
        
        # 执行构建命令
        return_code = run_command_realtime(
            full_command,
            stdout_callback=handle_stdout,
            stderr_callback=handle_stderr
        )
        
    finally:
        # 停止日志跟踪线程
        stop_event.set()
        log_thread.join(timeout=10)
    
    # 检查构建结果
    if return_code != 0:
        print("\n" + "="*60)
        print(f"Unity 构建失败! 退出码={return_code}")
        print("="*60)
        
        # 输出最后一部分日志（如果文件存在）
        if log_file.exists():
            print("\n最后 50 行日志:")
            print("-"*40)
            try:
                with open(log_file, 'r', encoding='utf-8', errors='replace') as f:
                    lines = f.readlines()
                    for line in lines[-50:]:
                        print(line.rstrip())
            except Exception as e:
                print(f"读取日志文件失败: {e}")
    
    return return_code

def main():
    """主函数"""
    # 检查参数数量
    if len(sys.argv) < 4:
        print("用法: python build_unity.py <Version> <BuildAddressable> <TargetPlatform> [CleanProject]")
        print("示例: python build_unity.py 1.0.0 true Windows true")
        sys.exit(1)
    
    # 解析参数
    version = sys.argv[1]
    build_addressable = sys.argv[2]
    target_platform = sys.argv[3]
    clean_project = sys.argv[4] if len(sys.argv) > 4 else "false"
    
    # 输出参数信息
    print(f"版本: {version}")
    print(f"构建 Addressable: {build_addressable}")
    print(f"目标平台: {target_platform}")
    print(f"清理项目: {clean_project}")
    
    # 环境变量
    project_dir = r"E:\Work\BuildDir\AutoBuild"
    git_url = "https://github.com/CultureLi/GameFramework.git"
    
    # 处理项目仓库
    print("\n" + "-"*40)
    print("处理工程目录")
    print("-"*40)
    
    if not handle_project_repository(project_dir, git_url, clean_project):
        print("处理项目仓库失败!")
        sys.exit(1)
    
    print("\n" + "-"*40)
    print("执行 Unity 构建")
    print("-"*40)
    
    # 执行 Unity 构建
    exit_code = build_unity_project(project_dir, version, build_addressable, target_platform)
    
    sys.exit(exit_code)

if __name__ == "__main__":
    main()