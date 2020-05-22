let conversationsArray;
let usersArray;
let user_id;
let currentConversation =
{
    id : null,
    type : null,
    currentPage : null
};
let currentQRFileName;


async function getConversations(userId) {
    user_id = userId;
    var form = new FormData();
    form.append("UserId", userId);
    var url = '/api/Conversations/GetConversations';
    const response = await fetch(url,
        {
            method: 'POST',
            body: form
        });
    var json = await response.text();
    json = json.replace(/([\[:])?(\d+)([,\}])/g, "$1\"$2\"$3");    
    await displayConversations(JSON.parse(json));
}
async function getMessages(conversationId, conversationType, messagesIds, pageNumber = 0) {
    var form = new FormData();
    form.append("ConversationId", conversationId);
    form.append("ConversationType", conversationType);
    if (messagesIds != null)
    {
        form.append("MessagesIds", messagesIds);
    }
    form.append("PageNumber", pageNumber);
    var url = '/api/Messages/GetMessages';    
    const response = await fetch(url,
        {
            method: 'POST',
            body: form
        });
    var json = await response.text();
    json = json.replace(/([\[:])?(\d+)([,\}])/g, "$1\"$2\"$3");
    return JSON.parse(json);
}
async function displayMessages(messagesModel) {
    messagesModel.messages = messagesModel.messages.reverse();
    var messagesElem = document.getElementById("messages");
    messagesElem.innerHTML = '';
    messagesModel.currentPage = parseInt(messagesModel.currentPage);
    currentConversation.currentPage =  messagesModel.currentPage;
    if (messagesModel.messages.length != 0) {
        messagesModel.messages.forEach(item => {
            var row = document.createElement("div");
            row.setAttribute("class", "list-group-item list-group-item-action media position-relative rounded");
            var mediaBody = document.createElement("div");
            mediaBody.classList.add("media-body");
            var messageText = document.createElement("p");
            messageText.appendChild(document.createTextNode(item.text));
            mediaBody.appendChild(messageText);
            var senderElem = document.createElement("a");
            senderElem.dataset.sender_id = item.senderId;
            senderElem.appendChild(document.createTextNode(item.senderId));
            senderElem.href = '#';
            row.appendChild(senderElem);
            row.appendChild(mediaBody);
            row.style.border = "none";
            messagesElem.appendChild(row);
        });        
        if (messagesModel.currentPage > 0)
        {
            var prevPageLink = document.getElementById("prev-page");
            var prevPageLinkClone = prevPageLink.cloneNode(true);
            prevPageLinkClone.addEventListener("click", async function () {
                var newMessages = await getMessages(currentConversation.id, currentConversation.type, null, messagesModel.currentPage - 1);
                await displayMessages(newMessages);
            }, { once: true });
            prevPageLink.parentNode.replaceChild(prevPageLinkClone, prevPageLink);
        }
        if (messagesModel.currentPage < messagesModel.pagesCount) {
            var nextPageLink = document.getElementById("next-page");
            var nextPageLinkClone = nextPageLink.cloneNode(true);
            nextPageLinkClone.addEventListener("click", async function () {
                var newMessages = await getMessages(currentConversation.id, currentConversation.type, null, messagesModel.currentPage + 1);
                await displayMessages(newMessages);
            }, { once: true });
            nextPageLink.parentNode.replaceChild(nextPageLinkClone, nextPageLink);
        }
        var currentPageLink = document.getElementById("current-page-link");
        currentPageLink.textContent = messagesModel.currentPage + 1;        
        messagesElem.scrollTo(0, messagesElem.scrollHeight);
    }
}
async function displayConversations(data) {
    var conversationsElem = document.getElementById("conversations");
    data.forEach(item =>
    {
        var row = document.createElement("div");
        var title = document.createElement("h6");
        title.classList.add("mt-0");
        title.appendChild(document.createTextNode(item.title));
        var mediaBody = document.createElement("div");
        mediaBody.classList.add("media-body");
        var previewText = document.createElement("p");
        previewText.appendChild(document.createTextNode(item.previewText));
        mediaBody.appendChild(previewText);
        row.setAttribute("class", "list-group-item list-group-item-action media position-relative");
        row.style.border = "none";
        row.dataset.conv_id = new BigNumber(item.conversationId);
        row.dataset.conv_type = new BigNumber(item.conversationType);   
        
        row.addEventListener("click", async function ()
        {
            this.parentElement.childNodes.forEach(r => r.classList.toggle("active", false));
            this.classList.toggle("active");
            await displayMessages(await getMessages(new BigNumber(row.dataset.conv_id), row.dataset.conv_type, null, 0));
            currentConversation = { id: new BigNumber(row.dataset.conv_id), type: row.dataset.conv_type, message_id: "" };
        });        
        row.appendChild(title); 
        row.appendChild(mediaBody);
        conversationsElem.appendChild(row);
    });
}
async function setUserBanned(userId) {
    var form = new FormData();
    form.append("UserId", userId);
    var url = '/api/Users/Ban';
    const response = await fetch(url,
        {
            method: 'POST',
            body: form
        });    
    return await response.json();
}
async function banUser_click(userId) {
    await setUserBanned(userId); 
    $('#modDialog').modal('hide');    
}
async function getQr(userId) {
    var url = 'api/qrcodes/GetQRCode?userId=' + userId;
    const response = await fetch(url,
        {
            method: 'GET'
        });
    var disposition = response.headers.get("Content-Disposition");
    var filename = getFilenameFromContentDisposition(disposition);
    var img = document.getElementById("qr-code-img");
    var blob = await response.blob();
    img.setAttribute("src", URL.createObjectURL(blob));
    var fileIdInput = document.getElementById("file-id");
    fileIdInput.setAttribute("value", filename);
    currentQRFileName = filename;
}
function getFilenameFromContentDisposition(disposition) 
{
    var filename = "";   
    if (disposition && disposition.indexOf('attachment') !== -1) {
        var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
        var matches = filenameRegex.exec(disposition);
        if (matches != null && matches[1]) {
            filename = matches[1].replace(/['"]/g, '');
        }
        return filename;
    }
}
async function sendQR(userId) {
    var email = document.getElementById("email").value;    
    var formData = new FormData();
    formData.append("userId", userId);
    formData.append("uploadFileId", currentQRFileName);
    formData.append("email", email);
    var url = "api/qrcodes/sendtoemail";
    var response = await fetch(url,
        {
            method : "POST",
            body : formData
        });
    try {
        var json = await response.json();
        if (json.length != 0) {
            alert(JSON.stringify(json));
        }
    }
    catch{
        $('#createQRModal').modal('hide');
    }
}

