import os
import sys
import re
import subprocess
import stat
import time
from pathlib import Path, PurePosixPath
from typing import List, Tuple, Optional, Dict
from getpass import getpass

# Конфигурация SFTP
SERVER_HOSTNAME = "r.dlike.ru"
SERVER_USERNAME = "admin"
SERVER_PORT = 22

def check_and_install_paramiko() -> bool:
    """
    Проверяет наличие paramiko и устанавливает при необходимости
    """
    try:
        import paramiko
        print(f"✓ paramiko уже установлен (v{paramiko.__version__})")
        return True
    except ImportError:
        try:
            print("Установка paramiko...")
            subprocess.check_call([sys.executable, "-m", "pip", "install", "paramiko"])
            print("✓ paramiko успешно установлен")
            return True
        except subprocess.CalledProcessError:
            print("✗ Ошибка установки paramiko. Установите вручную: pip install paramiko")
            return False

def connect_to_sftp(hostname: str, username: str, password: str) -> Optional[object]:
    """
    Подключается к серверу через SFTP
    """
    try:
        import paramiko
        transport = paramiko.Transport((hostname, SERVER_PORT))
        transport.connect(username=username, password=password)
        sftp = paramiko.SFTPClient.from_transport(transport)
        print(f"✓ Успешное подключение к {hostname}")
        return sftp, transport
    except Exception as e:
        print(f"✗ Ошибка подключения к {hostname}: {e}")
        return None

def check_file_via_sftp(sftp: object, file_path: str) -> bool:
    """
    Проверяет существование файла на сервере через SFTP
    """
    try:
        # Нормализуем путь для сервера
        server_path = file_path.replace('\\', '/')
        
        # Проверяем существование файла
        sftp.stat(server_path)
        return True
    except FileNotFoundError:
        return False
    except Exception:
        return False

def parse_pls_file(pls_path: Path) -> List[str]:
    """
    Парсит PLS файл и возвращает список путей к трекам
    Поддерживает формат: track=путь?;
    """
    tracks = []
    
    try:
        # Пробуем разные кодировки
        encodings = ['utf-8', 'cp1251', 'latin-1', 'utf-16']
        
        for encoding in encodings:
            try:
                with open(pls_path, 'r', encoding=encoding) as f:
                    content = f.read()
                    break
            except UnicodeDecodeError:
                continue
        else:
            # Если все кодировки не подошли, читаем как бинарный
            with open(pls_path, 'rb') as f:
                content = f.read().decode('utf-8', errors='ignore')
    
        # Ищем все строки с track= (поддерживаем разные форматы)
        # Форматы: track=путь?; или track=путь;
        lines = content.splitlines()
        
        for line in lines:
            line = line.strip()
            if not line:
                continue
                
            # Проверяем, начинается ли строка с track=
            if line.lower().startswith('track='):
                # Извлекаем путь после track=
                track_part = line[6:]  # Пропускаем "track="
                
                # Убираем вопросительный знак и точку с запятой в конце
                if track_part.endswith('?;'):
                    track_path = track_part[:-2]
                elif track_part.endswith(';'):
                    track_path = track_part[:-1]
                elif track_part.endswith('?'):
                    track_path = track_part[:-1]
                else:
                    track_path = track_part
                
                track_path = track_path.strip()
                if track_path:
                    tracks.append(track_path)
    
    except Exception as e:
        print(f"Ошибка чтения файла {pls_path}: {e}")
        return []
    
    return tracks

def check_missing_tracks(track_paths: List[str], 
                         use_sftp: bool = False,
                         sftp_connection: tuple = None) -> Tuple[List[str], List[str]]:
    """
    Проверяет существование файлов треков
    Возвращает два списка: существующие и отсутствующие треки
    Поддерживает локальную и SFTP проверку
    """
    existing_tracks = []
    missing_tracks = []
    
    # Если используется SFTP, извлекаем соединение
    sftp = None
    transport = None
    if use_sftp and sftp_connection:
        sftp, transport = sftp_connection
    
    for track_path in track_paths:
        found = False
        
        try:
            # Обрабатываем пути с экранированием
            track_path_clean = track_path
            if track_path_clean.startswith('"') and track_path_clean.endswith('"'):
                track_path_clean = track_path_clean[1:-1]
            
            # Проверяем существование файла
            if use_sftp and sftp:
                # Проверка через SFTP
                if check_file_via_sftp(sftp, track_path_clean):
                    existing_tracks.append(track_path)
                    found = True
            else:
                # Локальная проверка
                track_file = Path(track_path_clean)
                
                if track_file.exists() and track_file.is_file():
                    existing_tracks.append(track_path)
                    found = True
                else:
                    # Пробуем альтернативные пути
                    # 1. Без сетевого префикса
                    if track_path_clean.startswith('\\\\') or ':' in track_path_clean:
                        # Это уже абсолютный путь
                        pass
                    else:
                        # Пробуем как относительный путь от текущей директории
                        alt_path = Path.cwd() / track_path_clean
                        if alt_path.exists() and alt_path.is_file():
                            existing_tracks.append(track_path)
                            found = True
            
            # Если файл не найден
            if not found:
                missing_tracks.append(track_path)
                        
        except Exception as e:
            # В случае ошибки считаем файл отсутствующим
            missing_tracks.append(track_path)
    
    return existing_tracks, missing_tracks

def print_report(pls_file: str, all_tracks: List[str], existing_tracks: List[str], missing_tracks: List[str]):
    """
    Выводит отчет о проверке
    """
    print(f"\n{'='*60}")
    print("ОТЧЕТ О ПРОВЕРКЕ")
    print(f"{'='*60}")
    print(f"Плейлист: {pls_file}")
    print(f"Всего треков в плейлисте: {len(all_tracks)}")
    print(f"Существующие треки: {len(existing_tracks)}")
    print(f"Отсутствующие треки: {len(missing_tracks)}")
    
    if missing_tracks:
        print(f"\n{'='*60}")
        print("ОТСУТСТВУЮЩИЕ ТРЕКИ:")
        print(f"{'='*60}")
        
        for i, track in enumerate(missing_tracks, 1):
            print(f"{i:3d}. {track}")

def save_playlist(pls_path: Path, tracks: List[str], backup: bool = True) -> bool:
    """
    Сохраняет обновленный плейлист
    Поддерживает оригинальный формат: track=трек?;
    """
    if backup:
        # Создаем резервную копию
        backup_path = pls_path.with_name(pls_path.stem + "_backup" + pls_path.suffix)
        try:
            with open(pls_path, 'rb') as original:
                with open(backup_path, 'wb') as backup_file:
                    backup_file.write(original.read())
            print(f"\nСоздана резервная копия: {backup_path}")
        except Exception as e:
            print(f"Ошибка создания резервной копии: {e}")
            if input("Продолжить без резервной копии? (y/n): ").lower() != 'y':
                return False
    
    # Создаем новое содержимое плейлиста в оригинальном формате
    new_content = ""
    
    for track in tracks:
        # Сохраняем в оригинальном формате: track=путь?;
        # Проверяем, есть ли уже ? в конце
        if track.endswith('?'):
            new_content += f"track={track};\n"
        else:
            new_content += f"track={track}?;\n"
    
    # Сохраняем обновленный плейлист
    try:
        with open(pls_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        
        print(f"\n✓ Плейлист успешно обновлен: {pls_path}")
        return True
    
    except Exception as e:
        print(f"✗ Ошибка сохранения плейлиста: {e}")
        return False

def remove_missing_tracks_interactive(pls_path: Path, existing_tracks: List[str], missing_tracks: List[str]) -> bool:
    """
    Интерактивное удаление отсутствующих треков
    """
    if not missing_tracks:
        print("\nВсе треки существуют. Изменения не требуются.")
        return False
    
    print(f"\n{'='*60}")
    print("ОПЦИИ УДАЛЕНИЯ:")
    print(f"{'='*60}")
    print("1. Удалить все отсутствующие треки")
    print("2. Выбрать какие треки удалить")
    print("3. Не удалять треки")
    
    while True:
        choice = input("\nВыберите действие (1-3): ").strip()
        
        if choice == '1':
            # Удалить все отсутствующие
            confirm = input(f"Удалить все {len(missing_tracks)} отсутствующих треков? (y/n): ").lower()
            if confirm == 'y':
                return save_playlist(pls_path, existing_tracks, backup=True)
            else:
                print("Отменено пользователем")
                return False
                
        elif choice == '2':
            # Выборочное удаление
            print(f"\nВыберите треки для удаления (через запятую, 1-{len(missing_tracks)}):")
            for i, track in enumerate(missing_tracks, 1):
                print(f"{i:3d}. {track}")
            
            try:
                selection = input("\nНомера треков для удаления: ").strip()
                if not selection:
                    print("Отменено")
                    return False
                
                # Парсим выбранные номера
                indices_to_remove = set()
                for part in selection.split(','):
                    part = part.strip()
                    if '-' in part:
                        start, end = map(int, part.split('-'))
                        indices_to_remove.update(range(start, end + 1))
                    else:
                        indices_to_remove.add(int(part))
                
                # Проверяем валидность индексов
                valid_indices = [i for i in indices_to_remove if 1 <= i <= len(missing_tracks)]
                if not valid_indices:
                    print("Нет валидных номеров для удаления")
                    return False
                
                # Определяем какие треки сохранить
                tracks_to_keep = existing_tracks.copy()
                tracks_to_remove = []
                
                for idx in sorted(valid_indices, reverse=True):
                    tracks_to_remove.append(missing_tracks[idx-1])
                
                # Обновляем список существующих треков
                for track in missing_tracks:
                    if track not in tracks_to_remove:
                        tracks_to_keep.append(track)
                
                print(f"\nБудет удалено: {len(tracks_to_remove)} треков")
                print("Треки для удаления:")
                for track in tracks_to_remove:
                    print(f"  - {track}")
                
                confirm = input("\nПодтвердить удаление? (y/n): ").lower()
                if confirm == 'y':
                    return save_playlist(pls_path, tracks_to_keep, backup=True)
                else:
                    print("Отменено")
                    return False
                    
            except ValueError:
                print("Ошибка ввода. Введите номера через запятую или диапазон")
                continue
                
        elif choice == '3':
            print("Треки не будут удалены")
            return False
            
        else:
            print("Неверный выбор. Введите 1, 2 или 3")

def select_check_mode() -> Tuple[bool, Optional[tuple]]:
    """
    Выбор режима проверки: локальный или через SFTP
    """
    print(f"\n{'='*60}")
    print("ВЫБОР РЕЖИМА ПРОВЕРКИ")
    print(f"{'='*60}")
    print("1. Локальная проверка (файлы на локальном диске)")
    print("2. Удаленная проверка (файлы на сервере через SFTP)")
    
    while True:
        choice = input("\nВыберите режим проверки (1 или 2): ").strip()
        
        if choice == '1':
            return False, None
        elif choice == '2':
            # Настраиваем SFTP
            print(f"\n{'='*60}")
            print("НАСТРОЙКА SFTP ПОДКЛЮЧЕНИЯ")
            print(f"{'='*60}")
            
            # Проверяем наличие paramiko
            if not check_and_install_paramiko():
                print("Не удалось установить paramiko. Используйте локальную проверку.")
                return False, None
            
            # Запрашиваем параметры подключения
            hostname = input(f"Хост [{SERVER_HOSTNAME}]: ").strip() or SERVER_HOSTNAME
            username = input(f"Пользователь [{SERVER_USERNAME}]: ").strip() or SERVER_USERNAME
            password = getpass("Пароль: ")
            
            if not password:
                print("Пароль не может быть пустым")
                continue
            
            # Подключаемся к серверу
            sftp_connection = connect_to_sftp(hostname, username, password)
            if sftp_connection:
                return True, sftp_connection
            else:
                print("Не удалось подключиться. Попробуйте снова или выберите локальную проверку.")
        else:
            print("Неверный выбор. Введите 1 или 2")

def main():
    print(f"\n{'='*60}")
    print("ПРОВЕРКА И ОЧИСТКА ПЛЕЙЛИСТА")
    print("Формат: track=путь?;")
    print(f"{'='*60}")
    
    # Проверяем аргументы командной строки
    if len(sys.argv) != 2:
        print("\nИспользование:")
        print("python CheckPlaylist.py <путь_к_плейлисту.pls>")
        print("\nПример:")
        print('python CheckPlaylist.py "list.pls"')
        print('python CheckPlaylist.py "O:\\путь\\к\\playlist.pls"')
        return
    
    pls_path = Path(sys.argv[1])
    
    # Проверяем существование файла плейлиста
    if not pls_path.exists():
        print(f"\nОшибка: Файл плейлиста не найден: {pls_path}")
        print(f"Текущая директория: {Path.cwd()}")
        return
    
    if pls_path.suffix.lower() != '.pls':
        print(f"\nПредупреждение: Файл имеет расширение {pls_path.suffix}")
        if input("Продолжить обработку? (y/n): ").lower() != 'y':
            return
    
    # Выбираем режим проверки
    use_sftp, sftp_connection = select_check_mode()
    
    # Парсим плейлист
    print(f"\nЧтение плейлиста: {pls_path}")
    all_tracks = parse_pls_file(pls_path)
    
    if not all_tracks:
        print("Плейлист пуст или не содержит треков")
        if sftp_connection:
            sftp_connection[0].close()
            sftp_connection[1].close()
        return
    
    print(f"Найдено треков: {len(all_tracks)}")
    
    # Проверяем существование треков
    print("Проверка существования треков...")
    existing_tracks, missing_tracks = check_missing_tracks(all_tracks, use_sftp, sftp_connection)
    
    # Закрываем SFTP соединение если оно было открыто
    if sftp_connection:
        sftp_connection[0].close()
        sftp_connection[1].close()
    
    # Выводим отчет
    print_report(str(pls_path), all_tracks, existing_tracks, missing_tracks)
    
    # Если есть отсутствующие треки, предлагаем действия
    if missing_tracks:
        # Показываем меню действий
        success = remove_missing_tracks_interactive(pls_path, existing_tracks, missing_tracks)
        
        if success:
            print("\n✓ Плейлист успешно обновлен!")
        else:
            print("\n✗ Плейлист не был изменен")
    
    else:
        print(f"\n{'='*60}")
        print("✓ Все треки существуют. Изменения не требуются.")

if __name__ == "__main__":
    main()