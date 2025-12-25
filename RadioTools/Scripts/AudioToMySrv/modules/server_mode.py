import os
import json
import time
from pathlib import Path
from typing import List
from urllib.parse import quote

from .tag_reader import get_audio_tag, Tag
from .http_client import HttpClient

class ServerMode:
    def __init__(self, config: dict):
        self.config = config
        self.log_entries = []
        self.processed_files = []
        
        # Создаем папку для логов если нужно
        log_dir = os.path.dirname(config.get('log_file', 'process.log'))
        if log_dir and not os.path.exists(log_dir):
            os.makedirs(log_dir)
        
        self.process_songs()
    
    def scan_folder(self, folder: str) -> List[str]:
        """Сканирование папки с аудиофайлами"""
        if not os.path.exists(folder):
            return []
        
        extensions = set(self.config.get('supported_formats', 
                      ['.mp3', '.flac', '.wav']))
        
        audio_files = []
        for root, _, files in os.walk(folder):
            for file in files:
                if Path(file).suffix.lower() in extensions:
                    audio_files.append(os.path.join(root, file))
        
        return audio_files
    
    def create_server_path(self, song_path: str) -> str:
        """Создание пути на сервере"""
        filename = os.path.basename(song_path)
        server_path = self.config['server_path'].rstrip('/')
        return f"{server_path}/{filename}"
    
    def create_download_link(self, song_path: str) -> str:
        """Создание ссылки для скачивания"""
        filename = os.path.basename(song_path)
        download_link = self.config['download_link'].rstrip('/')
        return f"{download_link}/{filename}"
    
    def send_song(self, song_path: str, tag: Tag, attempt: int = 1) -> bool:
        """Отправка информации о песне на сервер"""
        if attempt > self.config['max_retries']:
            return False
        
        try:
            server_path = self.create_server_path(song_path)
            download_link = self.create_download_link(song_path)
            
            params = {
                "artist": tag.artist,
                "title": tag.title,
                "path": server_path,
                "link": download_link
            }
            
            result = HttpClient.send_data(
                self.config['page'],
                self.config['key'],
                params
            )
            
            # Логируем изменения кодировки
            if (tag.artist != quote(tag.artist) or 
                tag.title != quote(tag.title)):
                
                self.log_entries.extend([
                    f"Artist\t=\t{tag.artist}",
                    f"NArtist\t=\t{quote(tag.artist)}",
                    f"Title\t=\t{tag.title}",
                    f"NTitle\t=\t{quote(tag.title)}",
                    "-" * 40
                ])
            
            # Сохраняем информацию
            file_info = {
                "file": os.path.basename(song_path),
                "artist": tag.artist,
                "title": tag.title,
                "result": result
            }
            self.processed_files.append(file_info)
            
            return "Error" not in result
            
        except Exception:
            if attempt < self.config['max_retries']:
                time.sleep(3)
                return self.send_song(song_path, tag, attempt + 1)
            return False
    
    def process_songs(self):
        """Обработка всех песен в папке"""
        songs = self.scan_folder(self.config['songs_path'])
        
        if not songs:
            print("Аудиофайлы не найдены")
            return
        
        print(f"Найдено файлов: {len(songs)}")
        
        for i, song in enumerate(songs, 1):
            print(f"[{i}/{len(songs)}] {os.path.basename(song)}")
            
            tag = get_audio_tag(song)
            if tag:
                success = self.send_song(song, tag)
                print(f"  {'✓' if success else '✗'} {tag}")
            
            # Сохраняем логи
            if self.log_entries:
                self.save_logs()
        
        self.save_summary()
    
    def save_logs(self):
        """Сохранение логов в файл"""
        log_file = self.config.get('log_file', 'process.log')
        try:
            with open(log_file, 'w', encoding='utf-8') as f:
                f.write("\n".join(self.log_entries))
        except:
            pass
    
    def save_summary(self):
        """Сохранение сводного отчета"""
        summary = {
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
            "total_processed": len(self.processed_files),
            "files": self.processed_files
        }
        
        try:
            with open('summary.json', 'w', encoding='utf-8') as f:
                json.dump(summary, f, ensure_ascii=False, indent=2)
        except:
            pass