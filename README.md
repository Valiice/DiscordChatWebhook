# Discord Chat Webhook
**Simple, lightweight relay of game chat to Discord via Webhook.**

## 📝 Overview
This is a Dalamud plugin for *FINAL FANTASY XIV* that relays selected in-game chat messages directly to a Discord channel using a Webhook.

## ✨ Features
The plugin provides the following core features:

* **Chat Forwarding:** Relays in-game chat to a configurable Discord Webhook URL.
* **Selectable Channels:** Allows users to select which chat channels to forward, including: Say, Shout, Yell, Party, Free Company, Alliance, Incoming Tells, and Echo.
* **Duty Finder Notifications:** Includes an option to notify the Discord channel when a Duty Finder pop occurs.
* **Rich Discord Messaging:** Messages are sent as Discord Embeds, featuring color-coding based on the chat type (e.g., Party, Say, Tell).
* **Character Avatar Support:** Attempts to automatically fetch the sender's character avatar from the Lodestone for use as the webhook icon, using a default fallback image if necessary.

## 💻 Installation & Usage

### Prerequisites
* [**Dalamud**](https://github.com/goatcorp/Dalamud) (via **XIVLauncher**) is required to run this plugin.

### Installation
1.  Open the **Dalamud Settings** menu in-game.
2.  Navigate to the **Experimental** tab.
3.  Copy and paste the following URL into the **Custom Plugin Repositories** section:
    ```
    https://raw.githubusercontent.com/Valiice/DalamudPluginRepo/master/repo.json
    ```
4.  Click the **+** button to add the repository, then click **Save and Close**.
5.  Open the **Dalamud Plugin Installer** and search for **"Discord Chat Webhook"**.
6.  Click **Install**.

### Configuration
1.  **Open the configuration window** in-game by typing the command:
    ```
    /dcw
    ```
2.  **Enable the plugin** using the master switch.
3.  **Enter your Discord Webhook URL** into the provided input field.
4.  **Select the desired chat channels** to forward.
5.  **Enable 'Notify when Duty Finder Pops'** if you want automated notifications for duty readiness.

## 🛠️ Building from Source

This project uses the standard Dalamud development environment.

### Requirements
* [.NET SDK 9.0.x](https://dotnet.microsoft.com/download)
* A compatible version of the Dalamud dependencies (automatically downloaded by the build process).

### Build Steps
1.  **Clone the repository:**
    ```bash
    git clone https://github.com/Valiice/DalamudPluginRepo.git
    cd DiscordChatWebhook
    ```
2.  **Restore dependencies and build the project:**
    ```bash
    dotnet restore DiscordChatWebhook.slnx
    dotnet build --configuration Release DiscordChatWebhook/DiscordChatWebhook.csproj
    ```

The resulting plugin files can be found in `DiscordChatWebhook/bin/Release/`.

## 📜 Licensing
This project is licensed under the **GNU AFFERO GENERAL PUBLIC LICENSE Version 3** (AGPLv3). Please see the `LICENSE` file for full details.