#!/usr/bin/env python3
import json
import os
import sys

# Добавляем папку с модулями в путь
sys.path.append(os.path.join(os.path.dirname(__file__), 'modules'))

# Проверяем зависимости
from .modules.dependencies import check_and_install
if not check_and_install():
    sys.exit(1)

# Загружаем конфигурацию
try:
    with open('config.json', 'r', encoding='utf-8') as f:
        config = json.load(f)
except FileNotFoundError:
    print("Создайте файл config.json")
    sys.exit(1)

# Запускаем обработку
from .modules.server_mode import ServerMode

if __name__ == "__main__":
    try:
        print("Запуск обработки аудиофайлов...")
        server = ServerMode(config)
        print("Обработка завершена")
    except KeyboardInterrupt:
        print("\nПрервано пользователем")
    except Exception as e:
        print(f"Ошибка: {e}")