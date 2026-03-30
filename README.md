# 🚗 Car Insurance Telegram Bot

An automated Telegram bot designed to assist users in purchasing car insurance. The bot analyzes user documents (Passport and Vehicle Registration) using OCR and manages all interactions using AI communication.

---

## 🛠 Tech Stack

* **Framework:** .NET 9.0
* **Library:** Telegram.Bot (Official)
* **AI Engine:** Groq (using OpenAI Library) - There's also a fully working implementation for Google Gemini 2.5 Flash (using Mscc.GenerativeAI) and GPT
* **OCR Service:** Mindee API (Custom models for Passport and Vehicle Doc extraction)
* **Architecture:** State Machine Pattern, Dependency Injection, Asynchronous tasks

---

## 🚀 Setup & Installation

### 1. Prerequisites
* Install .NET 9 SDK.
* Obtain API Tokens from:
    * Telegram: @BotFather
    * Groq Cloud API Key(or Google AI Studio, or OpenAI)
    * Mindee: Dashboard

### 2. Configuration
Clone the repository and locate appsettings.json in the root directory. Fill in credentials

```json
{
  "ApiConfiguration": {
    "TelegramToken": "YOUR_TELEGRAM_BOT_TOKEN",
    "MindeeToken": "YOUR_MINDEE_API_KEY",
    "GeminiToken": "YOUR_GEMINI_API_KEY"
	"GroqToken" : "YOUR_GEMINI_API_KEY"
	"GeminiToken": "YOUR_GEMINI_API_KEY" (optional)
  },
  "MindeeConfig": {
    "PassportModelId": "YOUR_PASSPORT_MODEL_ID",
    "VehicleModelId": "YOUR_VEHICLE_MODEL_ID"
  }
}
```

### 3. Run the Bot
Open your terminal in the project folder and execute
dotnet run

---

## 🔄 Detailed Bot Workflow

The bot operates as a finite state machine to ensure a logical step-by-step user journey:

1. Introduction (/start): Bot resets the session and explains the process.
2. Passport Submission: User uploads a photo. Bot uses Mindee OCR to extract the Full Name and Document Number.
3. Vehicle Documents Submission: User uploads a photo of the registration certificate. Bot extracts the VIN Code and Car Model.
4. Data Confirmation: Bot shows the extracted data. 
    * If "Yes": Continues with pricing.
    * If "No": Resets the state to process the data again.
5. Price Agreement: Bot offers a a flat price of 100 USD. 
    * If Disagree: Explains the price is non-negotiable and offers to restart.
6. Policy Issuance: AI generates a personalized insurance policy and sends it to user.

---

## 💬 Interaction Flow Examples

### Successful Purchase Flow
* User: /start
* Bot: "Hello! I am your AI Car Insurance Assistant. To begin, please send a clear photo of your passport."
* User: *Sends passport photo*
* Bot: "I'm reading your data... Thank you, Ivan Ivanov! Now, please send your Vehicle Registration Certificate."
* User: *Sends vehicle doc photo*
* Bot: "Data extracted: Name: John Doe, VIN: 12345ABC, Model: BMW X5. Is this correct?"
* User: *Clicks "Yes, everything is correct"*
* Bot: "Great! The fixed insurance rate is 100 USD. Do you agree?"
* User: "Yes, I agree."
* Bot: "Excellent! Generating your policy... [Policy Content] ... Congratulations on your purchase!"

### ❌ Error/Correction Flow
* User: *Clicks "No, there are mistakes"*
* Bot: "I'm sorry about that! Let's try again. Please send a clearer photo of your passport with better lighting."

---

## 🛡 Security & Error Handling

* Prompt Externalization: All AI instructions are stored in appsettings.json using a PromptKeys constant system to avoid hardcoding.
* Fail-Safe: The bot includes try-catch blocks for API calls to prevent crashes.
* Environment Variables: Supports deployment via Environment Variables (on Railway/Azure) for secure key management.

---

## 🔗 Bot Link
t.me/carinsure_bot
