"""
Утилиты для работы с путями и URL
"""
import os


def create_download_link(file_path: str, songs_path: str, server_path: str, 
                        remove_prefix: str, base_url: str, relative_path: str = None) -> str:
    """
    Создает ссылку для скачивания файла
    
    Args:
        file_path: Локальный путь к файлу
        songs_path: Локальная папка с музыкой (из конфига)
        server_path: Базовый путь на сервере
        remove_prefix: Префикс пути, который нужно удалить
        base_url: Базовый URL для скачивания
        relative_path: Предварительно рассчитанный относительный путь
                      (используется в режиме drag-and-drop)
        
    Returns:
        Корректная ссылка для скачивания
    """
    # 1. Получаем относительный путь
    if relative_path is not None:
        # Используем предварительно рассчитанный путь (для drag-and-drop режима)
        # Убедимся, что путь использует правильные разделители
        rel_path = relative_path.replace('\\', '/')
    else:
        # Вычисляем относительный путь обычным способом
        try:
            rel_path = os.path.relpath(file_path, songs_path).replace('\\', '/')
        except ValueError:
            # Если пути на разных дисках, используем только имя файла как fallback
            rel_path = os.path.basename(file_path)
    
    # 2. Создаем полный серверный путь
    full_server_path = os.path.join(server_path, rel_path).replace('\\', '/')
    
    # 3. Находим позицию префикса в пути
    if remove_prefix:
        # Нормализуем пути
        path_to_check = full_server_path.replace('\\', '/')
        prefix_to_find = remove_prefix.replace('\\', '/')
        
        # Ищем префикс в пути
        idx = path_to_check.find(prefix_to_find)
        if idx != -1:
            # Берем путь после префикса
            filtered_path = path_to_check[idx + len(prefix_to_find):]
            # Удаляем начальный слеш если есть
            if filtered_path.startswith('/'):
                filtered_path = filtered_path[1:]
        else:
            filtered_path = path_to_check
    else:
        filtered_path = full_server_path.replace('\\', '/')
    
    # 4. Удаляем начальные слеши (на всякий случай)
    filtered_path = filtered_path.lstrip('/')
    
    # 5. Собираем финальную ссылку
    base_url = base_url.rstrip('/')
    if filtered_path:
        # Убедимся, что нет двойных слешей
        if base_url.endswith('/') or filtered_path.startswith('/'):
            full_url = f"{base_url.rstrip('/')}/{filtered_path.lstrip('/')}"
        else:
            full_url = f"{base_url}/{filtered_path}"
    else:
        full_url = base_url
    
    return full_url