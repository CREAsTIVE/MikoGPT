var cmid = $cmid$;
var replyId = $replyId$;
var peerId = $peerId$;
var counter = 0;

var name = API.messages.getByConversationMessageId({
	"peer_id": peerId,
	"conversation_message_ids": [cmid],
	"extended": 1,
	"fields": ["first_name", "last_name"]
}).profiles[0];

var arr = [[name.first_name + " " + name.last_name, "$$firstMessage$$"]];
var groupId = $groupId$;
var prefix = "$$prefix$$";
var prefixLen = prefix.length;

while (counter < 20 && replyId != null){
	var result = API.messages.getByConversationMessageId({
		"peer_id": peerId, 
		"conversation_message_ids": [replyId],
		"extended": 1, 
		"fields": ["first_name", "last_name"]
	});
	var msg = result.items[0];
	var lname = result.profiles[0].first_name + " " + result.profiles[0].last_name;

	replyId = msg.reply_message.conversation_message_id;

	var from = "assistant";
	if (msg.from_id != -groupId){
		from = lname;
	}
	arr.unshift([from, msg.text]);

	counter = counter + 1; 
}

return arr;