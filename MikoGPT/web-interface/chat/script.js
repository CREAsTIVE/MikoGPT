function getMessageObj(isLeft, text) {return `
<div class="message ${isLeft?"left":"right"}">
    <div class="text">${text}</div>
</div>
`}

var messagesObj = document.getElementById("messages-array");
var msgInp = document.getElementById("message-input");
var messages = [];
var publishKey = "1Su3Ztk9jhbzK4gxagLb5g==";

var sendMessage = () => {
    messages.push(["user", msgInp.value])
    if (msgInp.value != ""){
        messagesObj.innerHTML += getMessageObj(false, msgInp.value)
        msgInp.value="";
    }
    const options = {
        method: 'POST',
        headers: {'Content-Type': 'text/plain'},
        body: JSON.stringify({ key: publishKey, data: messages, version: 1 })//JSON.stringify(messages)
    };
    fetch('http://api.mikogpt.ru/chat', options)
        .then(response => response.json())
        .then(data => {
            messages.push(["assistant", data["output"]])
            messagesObj.innerHTML += getMessageObj(true, data["output"])
        })    
        .catch(error => console.log(error));
}

document.getElementById("send-message").onclick = sendMessage;
msgInp.addEventListener("keydown", (e) => {
    if (e.key == "Enter"){
        sendMessage();
    }
})Û