import os
from pathlib import Path
from typing import Optional
import taglib

class Tag:
    def __init__(self, artist: str, title: str):
        self.artist = artist
        self.title = title
    
    def __str__(self):
        return f"{self.artist} - {self.title}"

def get_audio_tag(song_path: str) -> Optional[Tag]:
    """Извлечение тегов из аудиофайла"""
    if not os.path.exists(song_path):
        return None
    
    try:
        audio = taglib.File(song_path)
        
        # Получаем исполнителя
        artist = "Unknown"
        if audio.tags.get('ARTIST'):
            artist = audio.tags['ARTIST'][0]
        elif audio.tags.get('ARTISTS'):
            artist = audio.tags['ARTISTS'][0]
        
        # Получаем название
        title = "Unknown"
        if audio.tags.get('TITLE'):
            title = audio.tags['TITLE'][0]
        
        # Если оба неизвестны - используем имя файла
        if artist == "Unknown" and title == "Unknown":
            title = Path(song_path).stem
        
        audio.close()
        return Tag(artist, title)
        
    except Exception:
        # При ошибке возвращаем тег с именем файла
        return Tag("Unknown", Path(song_path).stem)