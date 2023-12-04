import requests
import base64
import os
import sys
import subprocess


def main():
    CHUNK_SIZE = 1024
    # url = "https://api.elevenlabs.io/v1/text-to-speech/jBpfuIE2acCO8z3wKNLl"
    url = "https://api.elevenlabs.io/v1/text-to-speech/BRsaF2cpJGdkooZe0vZ9"

    headers = {
        "Accept": "audio/mpeg",
        "Content-Type": "application/json",
        "xi-api-key": os.getenv("xi-api-key")
    }

    data = {
        "text": sys.argv[1],
        "model_id": "eleven_monolingual_v1",
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75
        }
    }

    response = requests.post(url, json=data, headers=headers)

    with open(get_script_path() + "/output.mp3", "wb") as f:
        for chunk in response.iter_content(chunk_size=CHUNK_SIZE):
            if chunk:
                f.write(chunk)

    
    subprocess.call([get_script_path() + "/bin/ffmpeg.exe", "-y", "-i", get_script_path() + "/output.mp3", get_script_path() + "/output.wav"])
    output = base64.b64encode(open(get_script_path() + "/output.wav", "rb").read())
    output = str(output)
    output = output[2:-1]
    print(output)
    open(get_script_path() + "/output.txt", "w").write(output)


def get_script_path():
    return os.path.dirname(os.path.realpath(sys.argv[0]))


main()