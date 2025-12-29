#!/usr/bin/env python3
import json
import os
import sys

# Добавляем папку с модулями в путь
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'modules'))

# Проверяем зависимости
try:
    from modules.dependencies import check_and_install
except ImportError:
    print("Не удалось найти модуль dependencies в папке modules")
    sys.exit(1)

if not check_and_install():
    sys.exit(1)

# Загружаем конфигурацию
try:
    config_path = os.path.join(os.path.dirname(__file__), 'config.json')
    with open(config_path, 'r', encoding='utf-8') as f:
        config = json.load(f)
except FileNotFoundError:
    print("Создайте файл config.json")
    print("Скопируйте пример из config.example.json")
    sys.exit(1)
except json.JSONDecodeError as e:
    print(f"Ошибка в формате config.json: {e}")
    sys.exit(1)

# Запускаем обработку
try:
    from modules.unified_processor import UnifiedProcessor
except ImportError as e:
    print(f"Ошибка импорта unified_processor: {e}")
    sys.exit(1)

if __name__ == "__main__":
    try:
        print("=" * 60)
        print("Запуск объединенной обработки аудиофайлов...")
        print("=" * 60)
        
        # Определяем режим работы
        if len(sys.argv) > 1:
            # Режим drag-and-drop: обработка переданных файлов
            print(f"Режим: обработка {len(sys.argv)-1} переданных файлов")
            files_to_process = []
            for i in range(1, len(sys.argv)):
                file_path = sys.argv[i]
                if os.path.exists(file_path):
                    files_to_process.append(file_path)
                    print(f"  Добавлен файл: {file_path}")
                else:
                    print(f"  Предупреждение: файл не найден: {file_path}")
            
            if not files_to_process:
                print("Нет доступных файлов для обработки")
                sys.exit(0)
                
            processor = UnifiedProcessor(config)
            processor.run_drag_and_drop(files_to_process)
        else:
            # Режим по умолчанию: сканирование папки
            print("Режим: сканирование папки из конфигурации")
            processor = UnifiedProcessor(config)
            processor.run()
        
        print("=" * 60)
        print("Обработка завершена")
        print("=" * 60)
        
    except KeyboardInterrupt:
        print("\n\nПрервано пользователем")
    except Exception as e:
        print(f"\nКритическая ошибка: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)