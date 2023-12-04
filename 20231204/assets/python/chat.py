import openai
import sys
import json
import os


def main():
    openai.api_key = os.getenv("OPENAI_API_KEY")
    history = json.loads(sys.argv[1])

    response = openai.chat.completions.create(
        model="gpt-3.5-turbo",
        messages=history,
        tools=[
            {
                "type": "function",
                "function": {
                    "name": "response",
                    "description": "A response containing your reply and your emotions.",
                    "parameters": {
                        "type": "object",
                        "properties": {
                            "reply": {
                                "type": "string",
                                "description": "Your reply to the conversation."
                            },
                            "emotion": {
                                "type": "string",
                                "description": "Your emotion based on the conversation",
                                "enum": ["annoyed", "happy", "neutral", "sad", "confused", "curious", "tired"]
                            }
                        },
                        "required": ["reply", "emotion"]
                    }
                }
            }
        ],
        tool_choice={
            "type": "function",
            "function": {
                "name": "response"
            }
        }
    )

    print(response.choices[0].message.tool_calls[0].function.arguments)


main()