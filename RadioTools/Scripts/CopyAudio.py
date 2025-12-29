import os
import sys
import re
import subprocess
import shutil
import stat
import time
from pathlib import Path, PurePosixPath
from datetime import datetime
from getpass import getpass

AUDIO_EXTENSIONS = {'.mp3', '.wav', '.flac', '.m4a', '.aac', '.ogg', '.wma', '.opus'}
SERVER_HOSTNAME = "r.dlike.ru"
SERVER_USERNAME = "admin"
SERVER_PORT = 22

DATE_PREFIX_PATTERNS = [
    re.compile(r'^\d{4}-\d{2}-\d{2}_\d{2}-\d{2}_'),
    re.compile(r'^\d{4}-\d{2}-\d{2}_\d{1,2}-\d{2}_'),
    re.compile(r'^\d{4}-\d{2}-\d{2}_'),
    re.compile(r'^\d{4}-\d{2}-\d{2}\s+'),
]

def get_terminal_width():
    """Получить ширину терминала"""
    try:
        return shutil.get_terminal_size().columns
    except:
        return 80  # значение по умолчанию

def clear_line():
    """Очистить текущую строку в терминале"""
    width = get_terminal_width()
    print(f"\r{' ' * width}\r", end="", flush=True)

def print_progress(filename, current, total):
    """Отобразить прогресс копирования"""
    progress_text = f"Файл: {filename} ({current}/{total})"
    width = get_terminal_width()
    padding = max(0, width - len(progress_text))
    print(f"\r{progress_text}{' ' * padding}", end="", flush=True)

def print_header(title):
    width = get_terminal_width()
    print(f"\n{'=' * width}")
    print(title.center(width))
    print(f"{'=' * width}")

def print_report(title, new_count, skipped_count, new_tracks=None, skipped_tracks=None):
    print_header(title)
    print(f"Успешно скопировано: {new_count}")
    print(f"Пропущено: {skipped_count}")
    
    if new_tracks:
        print("\nСкопированные файлы:")
        for track in new_tracks:
            print(f"  ✓ {track}")
    
    if skipped_tracks:
        print("\nПропущенные файлы:")
        for track, normalized in skipped_tracks[:10]:
            print(f"  • {track} -> {normalized}")
        if len(skipped_tracks) > 10:
            print(f"  ... и еще {len(skipped_tracks) - 10} файлов")

def normalize_track_name(track_name):
    original_name = track_name
    
    for pattern in DATE_PREFIX_PATTERNS:
        match = pattern.match(original_name)
        if match:
            original_name = original_name[match.end():].strip()
    
    return original_name.lower() if original_name else track_name.lower()

def add_datetime_prefix(filename):
    return datetime.now().strftime("%Y-%m-%d_%H-%M_") + filename

def check_and_install_paramiko():
    try:
        import paramiko
        print(f"paramiko уже установлен (v{paramiko.__version__})")
        return True
    except ImportError:
        try:
            subprocess.check_call([sys.executable, "-m", "pip", "install", "paramiko"])
            return True
        except subprocess.CalledProcessError:
            print("Ошибка установки paramiko. Установите вручную: pip install paramiko")
            return False

def get_existing_tracks_local(target_folder):
    existing_tracks = {}
    target_path = Path(target_folder)
    
    if not target_path.exists():
        return existing_tracks
    
    try:
        for file_path in target_path.rglob('*'):
            if file_path.is_file() and file_path.suffix.lower() in AUDIO_EXTENSIONS:
                normalized_name = normalize_track_name(file_path.stem)
                existing_tracks[normalized_name] = file_path.name
    except Exception:
        pass
    
    return existing_tracks

def get_existing_tracks_remote(sftp, server_folder):
    existing_tracks = {}
    
    try:
        server_folder = server_folder.replace('\\', '/')
        sftp.stat(server_folder)
    except FileNotFoundError:
        print(f"Папка на сервере не существует: {server_folder}")
        return existing_tracks
    
    def scan_directory(current_path):
        try:
            for entry in sftp.listdir_attr(current_path):
                full_path = f"{current_path}/{entry.filename}" if current_path != '/' else f"/{entry.filename}"
                
                if stat.S_ISDIR(entry.st_mode):
                    scan_directory(full_path)
                else:
                    file_ext = Path(entry.filename).suffix.lower()
                    if file_ext in AUDIO_EXTENSIONS:
                        normalized_name = normalize_track_name(Path(entry.filename).stem)
                        existing_tracks[normalized_name] = entry.filename
        except Exception:
            pass
    
    scan_directory(server_folder)
    return existing_tracks

def get_audio_files_list(source_folder):
    """Получить список всех аудиофайлов в исходной папке"""
    source_path = Path(source_folder)
    audio_files = []
    
    if not source_path.exists():
        return audio_files
    
    for file_path in source_path.rglob('*'):
        if file_path.is_file() and file_path.suffix.lower() in AUDIO_EXTENSIONS:
            audio_files.append(file_path)
    
    return audio_files

def copy_files_local(source_folder, target_folder):
    source_path = Path(source_folder)
    target_path = Path(target_folder)
    
    if not source_path.exists():
        print(f"Исходная папка не существует: {source_folder}")
        return False
    
    target_path.mkdir(parents=True, exist_ok=True)
    existing_tracks = get_existing_tracks_local(target_folder)
    
    # Получаем список всех аудиофайлов
    audio_files = get_audio_files_list(source_folder)
    total_files = len(audio_files)
    
    if total_files == 0:
        print("В исходной папке не найдено аудиофайлов")
        return False
    
    print(f"\nНайдено аудиофайлов: {total_files}")
    
    new_tracks = []
    skipped_tracks = []
    processed = 0
    
    for file_path in audio_files:
        processed += 1
        track_name = file_path.stem
        normalized_name = normalize_track_name(track_name)
        
        # Отображаем прогресс
        print_progress(file_path.name, processed, total_files)
        
        if normalized_name in existing_tracks:
            skipped_tracks.append((file_path.name, normalized_name))
            continue
        
        relative_path = file_path.relative_to(source_path)
        target_file_path = target_path / relative_path
        
        new_filename = add_datetime_prefix(target_file_path.name)
        target_file_path = target_file_path.parent / new_filename
        
        target_file_path.parent.mkdir(parents=True, exist_ok=True)
        
        try:
            shutil.copy2(file_path, target_file_path)
            new_tracks.append(new_filename)
            existing_tracks[normalized_name] = new_filename
        except Exception as e:
            clear_line()
            print(f"\nОшибка при копировании {file_path.name}: {e}")
            continue
    
    # Очищаем строку прогресса
    clear_line()
    print_report("ЛОКАЛЬНОЕ КОПИРОВАНИЕ", len(new_tracks), len(skipped_tracks), new_tracks, skipped_tracks)
    return len(new_tracks) > 0 or len(skipped_tracks) > 0

def copy_files_remote(source_folder, server_folder, hostname, username, password):
    source_path = Path(source_folder)
    
    if not source_path.exists():
        print(f"Исходная папка не существует: {source_folder}")
        return False
    
    try:
        import paramiko
        transport = paramiko.Transport((hostname, SERVER_PORT))
        transport.connect(username=username, password=password)
        sftp = paramiko.SFTPClient.from_transport(transport)
    except Exception as e:
        print(f"Ошибка подключения: {e}")
        return False
    
    try:
        server_folder = server_folder.replace('\\', '/')
        sftp.stat(server_folder)
    except FileNotFoundError:
        print(f"Папка на сервере не существует: {server_folder}")
        return False
    
    existing_tracks = get_existing_tracks_remote(sftp, server_folder)
    
    # Получаем список всех аудиофайлов
    audio_files = get_audio_files_list(source_folder)
    total_files = len(audio_files)
    
    if total_files == 0:
        print("В исходной папке не найдено аудиофайлов")
        sftp.close()
        transport.close()
        return False
    
    print(f"\nНайдено аудиофайлов: {total_files}")
    
    new_tracks = []
    skipped_tracks = []
    processed = 0
    
    for file_path in audio_files:
        processed += 1
        normalized_name = normalize_track_name(file_path.stem)
        
        # Отображаем прогресс
        print_progress(file_path.name, processed, total_files)
        
        if normalized_name in existing_tracks:
            skipped_tracks.append((file_path.name, normalized_name))
            continue
        
        relative_path = file_path.relative_to(source_path)
        relative_posix = PurePosixPath(str(relative_path).replace('\\', '/'))
        
        new_filename = add_datetime_prefix(relative_posix.name)
        server_file_path = str(PurePosixPath(server_folder) / relative_posix.parent / new_filename)
        server_dir = str(PurePosixPath(server_file_path).parent).replace('\\', '/')
        server_file_path = server_file_path.replace('\\', '/')
        
        try:
            sftp.stat(server_dir)
        except FileNotFoundError:
            parent_dirs = []
            current_dir = server_dir
            
            while True:
                try:
                    sftp.stat(current_dir)
                    break
                except FileNotFoundError:
                    parent_dirs.append(current_dir)
                    current_dir = str(PurePosixPath(current_dir).parent)
                    if current_dir in ('/', '', '.'):
                        break
            
            for dir_to_create in reversed(parent_dirs):
                try:
                    sftp.mkdir(dir_to_create)
                except Exception:
                    continue
        
        try:
            time.sleep(0.1)
            sftp.put(str(file_path), server_file_path)
            new_tracks.append(new_filename)
            existing_tracks[normalized_name] = new_filename
        except Exception as e:
            clear_line()
            print(f"\nОшибка при копировании {file_path.name}: {e}")
            continue
    
    # Очищаем строку прогресса
    clear_line()
    print_report("УДАЛЕННОЕ КОПИРОВАНИЕ", len(new_tracks), len(skipped_tracks), new_tracks, skipped_tracks)
    
    sftp.close()
    transport.close()
    return len(new_tracks) > 0 or len(skipped_tracks) > 0

def select_mode():
    print_header("ВЫБОР РЕЖИМА РАБОТЫ")
    print("1. Локальное копирование (в другую локальную папку)")
    print("2. Удаленное копирование (на сервер через SFTP)")
    print(f"{'=' * get_terminal_width()}")
    
    while True:
        choice = input("Выберите режим (1 или 2): ").strip()
        if choice == '1':
            return 'local'
        elif choice == '2':
            return 'remote'
        else:
            print("Неверный выбор. Введите 1 или 2")

def main():
    print_header("АУДИО КОПИРОВАТЕЛЬ")
    
    mode = select_mode()
    
    if mode == 'remote':
        if not check_and_install_paramiko():
            print("Не удалось установить paramiko. Завершение работы.")
            sys.exit(1)
    
    if mode == 'local':
        if len(sys.argv) != 3:
            print("\nИспользование для локального копирования:")
            print("python CopyAudio.py <исходная_папка> <целевая_папка>")
            print("\nПример:")
            print('python CopyAudio.py "D:\\Music\\Source" "D:\\Music\\Backup"')
            return
        
        source_folder = sys.argv[1]
        target_folder = sys.argv[2]
        
        success = copy_files_local(source_folder, target_folder)
        
    else:
        if len(sys.argv) != 3:
            print("\nИспользование для удаленного копирования:")
            print("python CopyAudio.py <исходная_папка> <серверная_папка>")
            print("\nПример:")
            print('python CopyAudio.py "D:\\Music" "/home/admin/music"')
            return
        
        source_folder = sys.argv[1]
        server_folder = sys.argv[2]
        
        password = getpass("Введите пароль для сервера: ")
        
        if not password:
            print("Пароль не может быть пустым")
            sys.exit(1)
        
        success = copy_files_remote(
            source_folder, 
            server_folder, 
            SERVER_HOSTNAME, 
            SERVER_USERNAME, 
            password
        )
    
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()