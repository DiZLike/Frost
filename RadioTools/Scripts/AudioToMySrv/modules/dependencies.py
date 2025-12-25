import sys
import subprocess
import importlib

def check_and_install():
    """Проверка и установка зависимостей"""
    packages = ["requests", "pytaglib"]
    
    for package in packages:
        try:
            module = "taglib" if package == "pytaglib" else package
            importlib.import_module(module)
            print(f"✓ {package}")
        except ImportError:
            print(f"Установка {package}...")
            try:
                subprocess.check_call([sys.executable, "-m", "pip", "install", package])
            except subprocess.CalledProcessError:
                print(f"Ошибка установки {package}")
                if package == "pytaglib":
                    print("Требуется taglib:")
                    print("  Ubuntu: sudo apt install libtag1-dev")
                    print("  macOS: brew install taglib")
                    print("  Windows: скачайте с taglib.org")
                return False
    return True