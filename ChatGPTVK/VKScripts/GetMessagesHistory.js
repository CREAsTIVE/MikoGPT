var replyId = $replyId$;
var peerId = $peerId$;
var counter = 0;
var arr = [["user", "$$firstMessage$$"]];
var groupId = $groupId$;
var prefix = "$$prefix$$";
var prefixLen = prefix.length;

while (counter < 20 && replyId != null){
	var msg = API.messages.getByConversationMessageId({
		"peer_id": peerId, 
		"conversation_message_ids": [replyId],
		"extended": 0, 
		"fields": ""
	}).items[0];

	replyId = msg.reply_message.conversation_message_id;

	var from = "assistant";
	if (msg.from_id != -groupId){
		from = "user";
	}
	arr.unshift([from, msg.text]);

	counter = counter + 1; 
}

return arr;