var i = 1;
var messages = [];
var replyId = _firstMessageId_;

while (i < 20 && replyId != null) {
	var msg = API.messages.getByConversationMessageId({
		"peer_id": _peerId_,
		"conversation_message_ids": [replyId],
		"extended": 0,
		"fields": ""
	}).items[0];

	replyId = msg.reply_message.conversation_message_id;

	messages.unshift([msg.from_id, msg.text, msg.attachments]);

	i = i + 1;
}
return messages;