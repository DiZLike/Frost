"""
Объединенный процессор для работы с плейлистом и сервером
"""
import os
import re
import time
import requests
import json
from pathlib import Path
from typing import List, Set, Dict, Optional

from tag_reader import get_audio_tag, Tag
from http_client import HttpClient
from path_utils import create_download_link


class UnifiedProcessor:
    def __init__(self, config: dict):
        self.config = config
        self.existing_playlist_tracks = set()  # Нормализованные имена из плейлиста
        self.server_tracks_paths = set()  # Пути из БД сервера (нормализованные)
        self.new_files_to_add = []  # Новые файлы для добавления в плейлист
        self.files_for_server = []  # Файлы для отправки на сервер (новые в плейлисте + отсутствуют в БД)
        self.processed_tracks = []  # Отправленные треки на сервер
        self.playlist_duplicates = []  # Дубликаты в плейлисте
        self.server_duplicates = []  # Файлы уже в БД сервера (для информации)
        
        # Проверка обязательных параметров
        self._validate_config()
    
    def _validate_config(self):
        """Проверка обязательных параметров конфигурации"""
        required_params = [
            'songs_path', 'key', 'page', 'server_path',
            'playlist_file', 'server_tracks_url'
        ]
        
        missing = [param for param in required_params if param not in self.config]
        if missing:
            raise ValueError(f"Отсутствуют обязательные параметры в config.json: {missing}")
    
    def _normalize_track_name(self, track_name: str) -> str:
        """Удаляет дату в формате 2025-12-24 из названия трека"""
        # Удаляем дату в начале, середине или конце названия
        pattern = r'^\d{4}-\d{2}-\d{2}_\d{2}-\d{2}_'
        normalized = re.sub(pattern, ' ', track_name)
        # Удаляем лишние пробелы и приводим к нижнему регистру
        return ' '.join(normalized.split()).lower()
    
    def _get_existing_playlist_tracks(self) -> Set[str]:
        """Получает список уже добавленных треков из плейлиста"""
        existing_tracks = set()
        
        playlist_file = self.config['playlist_file']
        if not os.path.exists(playlist_file):
            print(f"Файл плейлиста не найден: {playlist_file}")
            print("Будет создан новый плейлист")
            return existing_tracks
        
        try:
            with open(playlist_file, 'r', encoding='utf-8') as f:
                content = f.read()
                # Ищем все треки в формате track=...?;
                tracks = re.findall(r'track=([^?\n]+)\?;', content)
                for track in tracks:
                    track = track.strip()
                    normalized = self._normalize_track_name(Path(track).stem)
                    existing_tracks.add(normalized)
            
            print(f"Найдено уникальных треков в плейлисте: {len(existing_tracks)}")
            return existing_tracks
            
        except Exception as e:
            print(f"Ошибка при чтении плейлиста: {e}")
            return existing_tracks
    
    def _scan_local_folder(self) -> List[str]:
        """Сканирование локальной папки с аудиофайлами"""
        songs_path = self.config['songs_path']
        
        if not os.path.exists(songs_path):
            raise ValueError(f"Локальная папка не существует: {songs_path}")
        
        extensions = set(self.config.get('supported_formats', 
                      ['.mp3', '.flac', '.wav', '.ogg', '.m4a', '.opus']))
        
        audio_files = []
        for root, _, files in os.walk(songs_path):
            for file in files:
                if Path(file).suffix.lower() in extensions:
                    audio_files.append(os.path.join(root, file))
        
        return audio_files
    
    def _add_to_playlist(self, file_path: str, relative_path: str = None) -> bool:
        """Добавляет файл в плейлист"""
        try:
            # Если relative_path не передан, рассчитываем обычным способом
            if relative_path is None:
                relative_path = os.path.relpath(file_path, self.config['songs_path'])
            
            server_file_path = os.path.join(
                self.config['server_path'], 
                relative_path
            ).replace('\\', '/')
            
            # Добавляем запись в плейлист
            playlist_file = self.config['playlist_file']
            mode = 'a' if os.path.exists(playlist_file) else 'w'
            
            with open(playlist_file, mode, encoding='utf-8') as f:
                playlist_entry = f"track={server_file_path}?;\n"
                f.write(playlist_entry)
            
            return True
            
        except Exception as e:
            print(f"Ошибка при добавлении в плейлист: {e}")
            return False
    
    def _get_server_tracks(self):
        """Получает список треков из БД сервера"""
        print("Получение списка треков с сервера...")
        
        try:
            response = requests.get(
                self.config['server_tracks_url'],
                timeout=30
            )
            
            if response.status_code != 200:
                raise RuntimeError(f"HTTP ошибка {response.status_code}: {response.text}")
            
            tracks_data = response.json()
            
            if not isinstance(tracks_data, list):
                raise RuntimeError(f"Некорректный ответ от сервера: {tracks_data}")
            
            # Извлекаем и нормализуем пути из БД сервера
            for track in tracks_data:
                if 'file_path' in track:
                    # Нормализуем путь для сравнения
                    path = track['file_path'].replace('\\', '/').strip()
                    self.server_tracks_paths.add(path)
            
            print(f"Получено треков с сервера: {len(self.server_tracks_paths)}")
            
        except requests.exceptions.RequestException as e:
            raise RuntimeError(f"Ошибка соединения с сервером: {e}")
        except json.JSONDecodeError as e:
            raise RuntimeError(f"Ошибка разбора JSON ответа сервера: {e}")
    
    def _normalize_server_path(self, local_path: str, relative_path: str = None) -> str:
        """Преобразует локальный путь в серверный путь для сравнения"""
        # Получаем относительный путь
        if relative_path is None:
            relative_path = os.path.relpath(local_path, self.config['songs_path'])
        
        # Создаем полный серверный путь
        server_file_path = os.path.join(
            self.config['server_path'], 
            relative_path
        ).replace('\\', '/')
        
        return server_file_path
    
    def _check_server_duplicate(self, file_path: str, relative_path: str = None) -> bool:
        """Проверяет, есть ли файл уже на сервере (в БД)"""
        server_path = self._normalize_server_path(file_path, relative_path)
        return server_path in self.server_tracks_paths
    
    def _process_playlist_phase(self, files: List[str] = None, drag_and_drop_mode: bool = False):
        """Фаза обработки плейлиста (поиск новых файлов для плейлиста)"""
        print("\n" + "=" * 50)
        print("Фаза 1: Поиск новых файлов для плейлиста")
        print("=" * 50)
        
        # Получаем существующие треки из плейлиста
        self.existing_playlist_tracks = self._get_existing_playlist_tracks()
        
        if files:
            # Используем переданные файлы (drag-and-drop режим)
            all_files = []
            extensions = set(self.config.get('supported_formats', 
                          ['.mp3', '.flac', '.wav', '.ogg', '.m4a', '.opus']))
            
            for file_path in files:
                if Path(file_path).suffix.lower() in extensions:
                    all_files.append(file_path)
                else:
                    print(f"  Пропуск (неподдерживаемый формат): {file_path}")
        else:
            # Сканируем локальную папку (режим по умолчанию)
            all_files = self._scan_local_folder()
            drag_and_drop_mode = False
        
        print(f"Найдено файлов для обработки: {len(all_files)}")
        
        # Ищем новые файлы ТОЛЬКО для плейлиста
        for file_path in all_files:
            track_name = Path(file_path).stem
            normalized_name = self._normalize_track_name(track_name)
            
            # Проверяем дубликаты в плейлисте
            if normalized_name in self.existing_playlist_tracks:
                print(f"  Пропуск (дубликат в плейлисте): '{track_name}'")
                self.playlist_duplicates.append(file_path)
                continue
            
            # Для режима drag-and-drop рассчитываем relative_path по-особому
            if drag_and_drop_mode:
                # Извлекаем путь вида "жанр\исполнитель\трек" из полного пути
                # Находим папки жанра и исполнителя в пути
                path_parts = Path(file_path).parts
                
                # Пытаемся найти структуру жанр/исполнитель
                # Обычно это предпоследняя и пред-предпоследняя папка
                if len(path_parts) >= 3:
                    # Берем последние 3 части пути (жанр/исполнитель/файл)
                    genre = path_parts[-3]
                    artist = path_parts[-2]
                    filename = path_parts[-1]
                    relative_path = f"{genre}/{artist}/{filename}"
                else:
                    # Если структура не соответствует, используем обычный метод
                    relative_path = os.path.relpath(file_path, self.config['songs_path'])
            else:
                relative_path = None
            
            # Добавляем в список новых файлов для плейлиста
            self.new_files_to_add.append((file_path, relative_path))
            self.existing_playlist_tracks.add(normalized_name)
        
        if self.playlist_duplicates:
            print(f"Пропущено дубликатов в плейлисте: {len(self.playlist_duplicates)}")
    
    def _prepare_server_files_phase(self, drag_and_drop_mode: bool = False):
        """Подготовка файлов для отправки на сервер (новые в плейлисте + отсутствуют в БД)"""
        print("\n" + "=" * 50)
        print("Фаза 2: Подготовка файлов для отправки на сервер")
        print("=" * 50)
        
        if not self.new_files_to_add:
            print("Нет новых файлов для плейлиста -> нет файлов для сервера")
            return
        
        # Проверяем каждый новый файл для плейлиста на наличие в БД сервера
        for file_path, relative_path in self.new_files_to_add:
            if self._check_server_duplicate(file_path, relative_path):
                # Файл уже есть в БД сервера
                print(f"  Пропуск (уже в БД сервера): {os.path.basename(file_path)}")
                self.server_duplicates.append(file_path)
            else:
                # Файла нет в БД - добавляем в список для отправки
                self.files_for_server.append((file_path, relative_path))
                print(f"  ✓ Для отправки на сервер: {os.path.basename(file_path)}")
        
        print(f"\nИтог подготовки файлов для сервера:")
        print(f"  Всего новых файлов для плейлиста: {len(self.new_files_to_add)}")
        print(f"  Файлов для отправки на сервер: {len(self.files_for_server)}")
        print(f"  Файлов уже в БД сервера: {len(self.server_duplicates)}")
    
    def _send_tracks_phase(self, drag_and_drop_mode: bool = False):
        """Фаза отправки треков на сервер (только новые файлы из плейлиста, отсутствующие в БД)"""
        if not self.files_for_server:
            print("\nНет файлов для отправки на сервер")
            return
        
        print("\n" + "=" * 50)
        print("Фаза 3: Отправка треков на сервер")
        print("=" * 50)
        
        success_count = 0
        error_count = 0
        
        for i, (file_path, relative_path) in enumerate(self.files_for_server, 1):
            print(f"\n[{i}/{len(self.files_for_server)}] Отправка: {os.path.basename(file_path)}")
            
            # Читаем теги
            tag = get_audio_tag(file_path)
            if not tag:
                print(f"  ✗ Ошибка чтения тегов")
                error_count += 1
                continue
            
            print(f"  Теги: {tag}")
            
            # Создаем download link с передачей relative_path
            download_link = create_download_link(
                file_path=file_path,
                songs_path=self.config['songs_path'],
                server_path=self.config['server_path'],
                remove_prefix=self.config.get('remove_prefix', ''),
                base_url=self.config.get('base_url', ''),
                relative_path=relative_path  # Передаем предварительно рассчитанный путь
            )
            
            # Отправляем на сервер
            server_file_path = self._normalize_server_path(file_path, relative_path)
            params = {
                "artist": tag.artist,
                "title": tag.title,
                "link": download_link,
                "file_path": server_file_path
            }
            
            result = HttpClient.send_data(
                self.config['page'],
                self.config['key'],
                params
            )
            
            # Сохраняем информацию
            file_info = {
                "file": os.path.basename(file_path),
                "full_path": file_path,
                "relative_path": relative_path,
                "download_link": download_link,
                "server_file_path": server_file_path,
                "artist": tag.artist,
                "title": tag.title,
                "result": result
            }
            
            self.processed_tracks.append(file_info)
            
            if "Error" in result:
                print(f"  ✗ Ошибка отправки: {result}")
                error_count += 1
            else:
                print(f"  ✓ Успешно отправлено на сервер")
                print(f"  Ссылка: {download_link}")
                success_count += 1
            
            # Добавляем паузу между запросами, если нужно
            time.sleep(0.5)
        
        # Вывод статистики
        print(f"\nСтатистика отправки на сервер:")
        print(f"  Успешно: {success_count}")
        print(f"  С ошибками: {error_count}")
    
    def _generate_report(self, mode: str = "default"):
        """Генерирует отчет о выполнении"""
        print("\n" + "=" * 60)
        print("ОТЧЕТ О ВЫПОЛНЕНИИ")
        print(f"Режим работы: {mode}")
        print("=" * 60)
        
        print(f"\nОбщая статистика:")
        print(f"  Добавлено в плейлист: {len(self.new_files_to_add)}")
        print(f"  Пропущено дубликатов в плейлисте: {len(self.playlist_duplicates)}")
        print(f"  Отправлено на сервер: {len(self.processed_tracks)}")
        print(f"  Уже было в БД сервера: {len(self.server_duplicates)}")
        
        success_count = len([t for t in self.processed_tracks if 'Error' not in t['result']])
        error_count = len([t for t in self.processed_tracks if 'Error' in t['result']])
        print(f"  Успешно отправлено: {success_count}")
        print(f"  Ошибок отправки: {error_count}")
        
        if self.new_files_to_add:
            print(f"\nДобавленные в плейлист файлы:")
            for i, (file_path, relative_path) in enumerate(self.new_files_to_add[:10]):  # Показываем первые 10
                status = "✓" if (file_path, relative_path) in self.files_for_server else "✗ (уже в БД)"
                display_path = relative_path if relative_path else os.path.basename(file_path)
                print(f"  {status} {display_path}")
            if len(self.new_files_to_add) > 10:
                print(f"  ... и еще {len(self.new_files_to_add) - 10}")
        
        if self.playlist_duplicates:
            print(f"\nПропущенные дубликаты в плейлисте:")
            for dup in self.playlist_duplicates[:5]:
                print(f"  - {os.path.basename(dup)}")
            if len(self.playlist_duplicates) > 5:
                print(f"  ... и еще {len(self.playlist_duplicates) - 5}")
        
        # Сохраняем подробный отчет в файл
        report_file = f"unified_processor_report_{time.strftime('%Y%m%d_%H%M%S')}.txt"
        try:
            with open(report_file, 'w', encoding='utf-8') as f:
                f.write("=" * 60 + "\n")
                f.write("ОТЧЕТ UNIFIED PROCESSOR\n")
                f.write(f"Режим работы: {mode}\n")
                f.write(f"Время выполнения: {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
                f.write("=" * 60 + "\n\n")
                
                f.write(f"Общая статистика:\n")
                f.write(f"  Добавлено в плейлист: {len(self.new_files_to_add)}\n")
                f.write(f"  Пропущено дубликатов в плейлисте: {len(self.playlist_duplicates)}\n")
                f.write(f"  Отправлено на сервер: {len(self.processed_tracks)}\n")
                f.write(f"  Уже было в БД сервера: {len(self.server_duplicates)}\n")
                f.write(f"  Успешно отправлено: {success_count}\n")
                f.write(f"  Ошибок отправки: {error_count}\n\n")
                
                if self.new_files_to_add:
                    f.write("Добавленные в плейлист:\n")
                    for file_path, relative_path in self.new_files_to_add:
                        status = "✓ ДЛЯ СЕРВЕРА" if (file_path, relative_path) in self.files_for_server else "✗ УЖЕ В БД"
                        display_path = relative_path if relative_path else os.path.basename(file_path)
                        f.write(f"  {status} {display_path}\n")
                        f.write(f"     Полный путь: {file_path}\n\n")
                
                if self.processed_tracks:
                    f.write("Отправленные на сервер треки:\n")
                    for track in self.processed_tracks:
                        status = "✓" if "Error" not in track['result'] else "✗"
                        f.write(f"  {status} {track['artist']} - {track['title']}\n")
                        f.write(f"     Файл: {track['file']}\n")
                        f.write(f"     Относительный путь: {track['relative_path']}\n")
                        f.write(f"     Результат: {track['result']}\n\n")
                
                if self.playlist_duplicates:
                    f.write("\nПропущенные дубликаты в плейлисте:\n")
                    for dup in self.playlist_duplicates:
                        f.write(f"  - {dup}\n")
                
                if self.server_duplicates:
                    f.write("\nФайлы уже имеющиеся в БД сервера:\n")
                    for dup in self.server_duplicates:
                        f.write(f"  - {dup}\n")
            
            print(f"\nПодробный отчет сохранен в: {report_file}")
            
        except Exception as e:
            print(f"Ошибка при сохранении отчета: {e}")
    
    def run_drag_and_drop(self, files: List[str]):
        """Основной метод для режима drag-and-drop"""
        print(f"Обработка {len(files)} файлов в режиме drag-and-drop")
        
        # 1. Получаем треки с сервера
        self._get_server_tracks()
        
        # 2. Обрабатываем плейлист (ищем новые файлы для плейлиста)
        self._process_playlist_phase(files, drag_and_drop_mode=True)
        
        # 3. Добавляем новые файлы в плейлист
        if self.new_files_to_add:
            print("\n" + "=" * 50)
            print("Добавление новых файлов в плейлист...")
            print("=" * 50)
            
            added_count = 0
            for file_path, relative_path in self.new_files_to_add:
                if self._add_to_playlist(file_path, relative_path):
                    display_path = relative_path if relative_path else os.path.basename(file_path)
                    print(f"  ✓ Добавлено в плейлист: {display_path}")
                    added_count += 1
                else:
                    print(f"  ✗ Ошибка добавления в плейлист: {os.path.basename(file_path)}")
            
            print(f"\nДобавлено в плейлист: {added_count} из {len(self.new_files_to_add)}")
        else:
            print("\nНет новых файлов для добавления в плейлист")
        
        # 4. Подготавливаем файлы для отправки на сервер
        self._prepare_server_files_phase(drag_and_drop_mode=True)
        
        # 5. Отправляем файлы на сервер
        self._send_tracks_phase(drag_and_drop_mode=True)
        
        # 6. Генерируем отчет
        self._generate_report(mode="drag-and-drop")
    
    def run(self):
        """Основной метод выполнения всех фаз (режим по умолчанию)"""
        # 1. Получаем треки с сервера
        self._get_server_tracks()
        
        # 2. Обрабатываем плейлист (ищем новые файлы для плейлиста)
        self._process_playlist_phase()
        
        # 3. Добавляем новые файлы в плейлист
        if self.new_files_to_add:
            print("\n" + "=" * 50)
            print("Добавление новых файлов в плейлист...")
            print("=" * 50)
            
            added_count = 0
            for file_path, relative_path in self.new_files_to_add:
                if self._add_to_playlist(file_path, relative_path):
                    print(f"  ✓ Добавлено в плейлист: {os.path.basename(file_path)}")
                    added_count += 1
                else:
                    print(f"  ✗ Ошибка добавления в плейлист: {os.path.basename(file_path)}")
            
            print(f"\nДобавлено в плейлист: {added_count} из {len(self.new_files_to_add)}")
        else:
            print("\nНет новых файлов для добавления в плейлист")
        
        # 4. Подготавливаем файлы для отправки на сервер
        self._prepare_server_files_phase()
        
        # 5. Отправляем файлы на сервер
        self._send_tracks_phase()
        
        # 6. Генерируем отчет
        self._generate_report(mode="default")