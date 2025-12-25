import requests
from urllib.parse import quote

class HttpClient:
    @staticmethod
    def send_data(page: str, key: str, params: dict) -> str:
        """Отправка GET-запроса на сервер"""
        try:
            # Формируем параметры URL
            url_params = f"key={quote(key)}"
            for param_name, param_value in params.items():
                url_params += f"&{param_name}={quote(str(param_value))}"
            
            # Отправляем запрос
            url = f"{page}?{url_params}"
            response = requests.get(url, timeout=30)
            
            if response.status_code == 200:
                return response.text
            else:
                return f"HTTP Error: {response.status_code}"
                
        except requests.exceptions.RequestException as e:
            return f"Request Error: {str(e)}"