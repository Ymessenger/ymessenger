(window.webpackJsonp=window.webpackJsonp||[]).push([[0],{130:function(e,t,a){e.exports=a.p+"static/media/logo.024bbe57.PNG"},179:function(e,t,a){e.exports=a(347)},185:function(e,t,a){},347:function(e,t,a){"use strict";a.r(t);var n=a(0),r=a.n(n),s=a(4),l=a.n(s),o=(a(184),a(185),a(15)),c=a(57),i=a(164),u=a(37);var d=Object(c.c)({dialogs:function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:[],t=arguments.length>1?arguments[1]:void 0;if("DIALOGS_RECEIVED"===t.type)return Object(u.a)(e),t.dialogs;if("OPEN_DIALOG"===t.type){var a=Object(u.a)(e);return a.map(function(e){e.active=!1,e.ConversationId===t.dialogId&&(e.active=!0)}),a}if("MESSAGES_RECEIVED"===t.type){var n=Object(u.a)(e);if(t.messages.length){var r=t.messages[0].ConversationId;n.map(function(e){e.ConversationId===r&&(e.messagesRecieved=!0)})}return n}return e},messages:function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:[],t=arguments.length>1?arguments[1]:void 0;if("MESSAGES_RECEIVED"===t.type){var a=Object(u.a)(e);return(a=[].concat(Object(u.a)(a),Object(u.a)(t.messages))).sort(function(e,t){var a=new Date(e.SendingTime),n=new Date(t.SendingTime);return a<n?-1:a>n?1:0})}return e},users:function(){var e=arguments.length>0&&void 0!==arguments[0]?arguments[0]:[],t=arguments.length>1?arguments[1]:void 0;if("USER_RECEIVED"===t.type){var a=e;return a.find(function(e){return e.Id===t.users[0].Id})||(a=[].concat(Object(u.a)(a),Object(u.a)(t.users))),a}return e},forwardMessage:function(){var e=arguments.length>0&&void 0!==arguments[0]&&arguments[0],t=arguments.length>1?arguments[1]:void 0;if("FORWARD_MESSAGE"===t.type)return t.message;return e}});var m=a(9);function f(e){return{type:"OPEN_DIALOG",dialogId:e}}var g=a(20),p=a(49),v=a(46),h=a(47),E=a(66),b=a(62),y=a(65),O=a(356),T=a(353),j=a(354),S=a(29),k=a(361),I=a(10),C=function(e){function t(){var e;return Object(v.a)(this,t),(e=Object(E.a)(this,Object(b.a)(t).call(this))).state={nodes:[],activeNode:null},e}return Object(y.a)(t,e),Object(h.a)(t,[{key:"componentDidMount",value:function(){var e=this;fetch("https://223421.selcdn.ru/Eruption-dev/nodes/list.json").then(function(t){t.json().then(function(t){e.setState({nodes:t})})}).catch(function(e){console.log(e)})}},{key:"selectNode",value:function(e){this.setState({activeNode:e.item.props.data})}},{key:"render",value:function(){var e=r.a.createElement(O.b,{onClick:this.selectNode.bind(this)},this.state.nodes.map(function(e,t){return r.a.createElement(O.b.Item,{key:t,data:e},e.Name)}),this.state.activeNode?r.a.createElement(O.b.Item,{key:"auto",data:null},"\u0410\u0432\u0442\u043e\u043c\u0430\u0442\u0438\u0447\u0435\u0441\u043a\u0438"):null);return r.a.createElement(T.a,{type:"flex",align:"middle",justify:"center",style:{height:"100vh"}},r.a.createElement(j.a,null,r.a.createElement("div",{className:"auth__type"},r.a.createElement("h1",null,"\u0412\u0445\u043e\u0434"),r.a.createElement("h2",null,"\u0412\u044b\u0431\u0435\u0440\u0438\u0442\u0435 \u0441\u043f\u043e\u0441\u043e\u0431 \u0432\u0445\u043e\u0434\u0430"),r.a.createElement(g.b,{to:this.props.match.url+"/by-phone"},r.a.createElement(S.a,{type:"secondary"},"\u041f\u043e \u043d\u043e\u043c\u0435\u0440\u0443 \u0442\u0435\u043b\u0435\u0444\u043e\u043d\u0430")),r.a.createElement("br",null),r.a.createElement(S.a,{type:"secondary"},"\u041f\u043e \u044d\u043b\u0435\u043a\u0442\u0440\u043e\u043d\u043d\u043e\u0439 \u043f\u043e\u0447\u0442\u0435"),r.a.createElement("br",null),r.a.createElement(S.a,{type:"secondary"},"\u0427\u0435\u0440\u0435\u0437 \u043f\u0440\u0438\u043b\u043e\u0436\u0435\u043d\u0438\u0435"),r.a.createElement("br",null),r.a.createElement("br",null),r.a.createElement(g.b,{to:"/registration"},"\u0420\u0435\u0433\u0438\u0441\u0442\u0440\u0430\u0446\u0438\u044f"),r.a.createElement("br",null),r.a.createElement("div",{className:"auth__type_server"},r.a.createElement("span",null,"\u0421\u0435\u0440\u0432\u0435\u0440: "),r.a.createElement(k.a,{overlay:e},r.a.createElement("a",{className:"ant-dropdown-link",href:"#"},this.state.activeNode?this.state.activeNode.Name:"\u0410\u0432\u0442\u043e\u043c\u0430\u0442\u0438\u0447\u0435\u0441\u043a\u0438"," ",r.a.createElement(I.a,{type:"down"})))))))}}]),t}(n.Component),R=a(358),_=a(360),N=new WebSocket("wss://testnode1.ymess.org:5000"),w=new(function(){function e(){Object(v.a)(this,e),N.onopen=function(){console.log("\u0421\u043e\u0435\u0434\u0438\u043d\u0435\u043d\u0438\u0435 \u0443\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u043e"),this.wsOnConnect()}.bind(this),N.onclose=function(e){e.wasClean?console.log("\u0421\u043e\u0435\u0434\u0438\u043d\u0435\u043d\u0438\u0435 \u0437\u0430\u043a\u0440\u044b\u0442\u043e \u0447\u0438\u0441\u0442\u043e"):console.log("\u041e\u0431\u0440\u044b\u0432 \u0441\u043e\u0435\u0434\u0438\u043d\u0435\u043d\u0438\u044f"),console.log("\u041a\u043e\u0434: ",e)},N.onmessage=function(e){this.onMessage(this.blobToJson(e.data))}.bind(this),N.onerror=function(e){console.log("\u041e\u0448\u0438\u0431\u043a\u0430 ",e.message)}}return Object(h.a)(e,[{key:"getStatus",value:function(){return N.readyState}},{key:"blobToJson",value:function(e){var t,a;return t=URL.createObjectURL(e),(a=new XMLHttpRequest).open("GET",t,!1),a.send(),URL.revokeObjectURL(t),JSON.parse(a.responseText)}},{key:"makeId",value:function(){for(var e="",t="0123456789".length,a=0;a<18;a++)e+="0123456789".charAt(Math.floor(Math.random()*t));return parseInt(e)}},{key:"send",value:function(e){N.send(JSON.stringify(e))}},{key:"onMessage",value:function(){}},{key:"wsOnConnect",value:function(){}}]),e}()),A=a(11),M=a.n(A);var x=function(){var e=this,t=Object(n.useState)(""),a=Object(m.a)(t,2),s=a[0],l=a[1],o=Object(n.useState)(""),c=Object(m.a)(o,2),i=c[0],u=c[1],d=Object(n.useState)(""),f=Object(m.a)(d,2),g=f[0],p=f[1],v=Object(n.useState)(""),h=Object(m.a)(v,2),E=h[0],b=h[1],y=Object(n.useState)(!1),O=Object(m.a)(y,2),k=O[0],I=O[1],C=Object(n.useState)(!1),N=Object(m.a)(C,2),A=N[0],x=N[1],U=Object(n.useState)(!1),P=Object(m.a)(U,2),D=P[0],q=P[1];return Object(n.useEffect)(function(){w.onMessage=function(e){0!==e.ErrorCode?console.log(e):6===e.ResponseType&&(M.a.set("FileAccessToken",e.FileAccessToken),M.a.set("AccessToken",e.Token.AccessToken),M.a.set("RefreshToken",e.Token.RefreshToken),M.a.set("User",e.User),window.location.replace("/"))}.bind(e)},[]),r.a.createElement(T.a,{type:"flex",align:"middle",justify:"center",style:{height:"100vh"}},r.a.createElement(j.a,null,r.a.createElement("div",{className:"auth__type"},r.a.createElement("h1",null,"\u0420\u0435\u0433\u0438\u0441\u0442\u0440\u0430\u0446\u0438\u044f"),r.a.createElement("div",null,r.a.createElement(R.a,{className:"login-form"},r.a.createElement(R.a.Item,{validateStatus:k?"error":"",help:k||""},r.a.createElement(_.a,{placeholder:"\u0418\u043c\u044f",onChange:function(e){l(e.target.value),I(!1)}})),r.a.createElement(R.a.Item,{validateStatus:""},r.a.createElement(_.a,{placeholder:"\u0424\u0430\u043c\u0438\u043b\u0438\u044f",onChange:function(e){u(e.target.value)}})),r.a.createElement(R.a.Item,{validateStatus:A?"error":"",help:A||""},r.a.createElement(_.a,{placeholder:"\u041d\u043e\u043c\u0435\u0440 \u0442\u0435\u043b\u0435\u0444\u043e\u043d\u0430",onChange:function(e){p(e.target.value),x(!1)}})),r.a.createElement(R.a.Item,{validateStatus:A?"error":"",help:A||""},r.a.createElement(_.a,{placeholder:"Email",onChange:function(e){b(e.target.value),x(!1)}})),r.a.createElement(R.a.Item,null,r.a.createElement(T.a,null,r.a.createElement(j.a,{style:{textAlign:"center"}},r.a.createElement(S.a,{type:"primary",onClick:function(){if(0===s.length)I("\u042d\u0442\u043e \u043f\u043e\u043b\u0435 \u043e\u0431\u044f\u0437\u0430\u0442\u0435\u043b\u044c\u043d\u043e \u0434\u043b\u044f \u0437\u0430\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f");else if(0===g.length&&0===E.length)x("\u041e\u0434\u043d\u043e \u0438\u0437 \u044d\u0442\u0438\u0445 \u043f\u043e\u043b\u0435\u0439 \u043e\u0431\u044f\u0437\u0430\u0442\u0435\u043b\u044c\u043d\u043e \u0434\u043b\u044f \u0437\u0430\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f");else{q(!0);var e={User:{Id:0,Phones:null,NameFirst:s,NameSecond:i,About:"",Photo:"",Country:"",City:"",Birthday:"",Language:null,NodeId:0,Online:null,Emails:null,Blacklist:null,Visible:null,Security:null,Tag:0,RegistrationDate:null},RequestId:w.makeId(),RequestType:37,Type:0};w.send(e)}},loading:D},"\u0417\u0430\u0440\u0435\u0433\u0438\u0441\u0442\u0440\u0438\u0440\u043e\u0432\u0430\u0442\u044c\u0441\u044f")))))))))},U=a(355),P=(a(332),function(e){function t(){var e;return Object(v.a)(this,t),(e=Object(E.a)(this,Object(b.a)(t).call(this))).state={phoneNumber:null,phoneNumberErr:null,phoneNumberSended:!1,verificationCode:null,verificationCodeErr:null,redirect:!1,btnPreloader:!1},e}return Object(y.a)(t,e),Object(h.a)(t,[{key:"componentDidMount",value:function(){w.onMessage=function(e){this.setState({btnPreloader:!1}),0!==e.ErrorCode?this.setState({phoneNumberErr:e.Message}):10===e.ResponseType?this.setState({phoneNumberSended:!0}):6===e.ResponseType&&(console.log(e),M.a.set("FileAccessToken",e.FileAccessToken),M.a.set("AccessToken",e.Token.AccessToken),M.a.set("RefreshToken",e.Token.RefreshToken),M.a.set("User",e.User),window.location.replace("/"))}.bind(this)}},{key:"sendVerificationQuery",value:function(e){e.preventDefault(),this.setState({btnPreloader:!0});var t={VerificationType:0,Uid:this.state.phoneNumber,RequestId:w.makeId(),RequestType:42,Type:0};w.send(t)}},{key:"sendVerificationCode",value:function(e){e.preventDefault(),this.setState({btnPreloader:!0});var t={Uid:this.state.phoneNumber,VCode:this.state.verificationCode,LoginType:1,UidType:0,RequestId:w.makeId(),RequestType:33,Type:0};w.send(t)}},{key:"onChangePhone",value:function(e){this.setState({phoneNumber:e.target.value,phoneNumberErr:null})}},{key:"onChangeVerifCode",value:function(e){this.setState({verificationCode:e.target.value,verificationCodeErr:null})}},{key:"render",value:function(){return r.a.createElement(T.a,{type:"flex",align:"middle",justify:"center",style:{height:"100vh"}},r.a.createElement(j.a,null,r.a.createElement("div",{className:"auth__type"},r.a.createElement("h1",null,"\u0412\u0445\u043e\u0434"),this.state.phoneNumberSended?r.a.createElement("div",null,r.a.createElement("h2",null,"\u041a\u043e\u0434 \u043f\u043e\u0434\u0442\u0432\u0435\u0440\u0436\u0434\u0435\u043d\u0438\u044f \u0438\u0437 SMS"),r.a.createElement("p",null,"\u041c\u044b \u043e\u0442\u043f\u0440\u0430\u0432\u0438\u043b\u0438 SMS \u043d\u0430 \u043d\u043e\u043c\u0435\u0440 ",this.state.phoneNumber,". \u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u043f\u043e\u043b\u0443\u0447\u0435\u043d\u044b\u0439 \u043a\u043e\u0434 \u0432 \u043f\u043e\u043b\u0435 \u043d\u0438\u0436\u0435"),r.a.createElement(R.a,{onSubmit:this.sendVerificationCode.bind(this),className:"login-form"},r.a.createElement(R.a.Item,{validateStatus:this.state.verificationCodeErr?"error":"",help:this.state.verificationCodeErr?this.state.verificationCodeErr:""},r.a.createElement(_.a,{placeholder:"\u041a\u043e\u0434 \u0438\u0437 SMS, 4 \u0446\u0438\u0444\u0440\u044b, \u043a\u0430\u043a \u043f\u0430\u0440\u043e\u043b\u044c",onChange:this.onChangeVerifCode.bind(this)})),r.a.createElement(R.a.Item,null,r.a.createElement(T.a,null,r.a.createElement(j.a,{style:{textAlign:"center"}},r.a.createElement(S.a,{type:"primary",htmlType:"submit",disabled:!this.state.verificationCode,loading:!!this.state.btnPreloader},"\u0412\u043e\u0439\u0442\u0438")))))):r.a.createElement("div",null,r.a.createElement("h2",null,"\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u043d\u043e\u043c\u0435\u0440 \u0442\u0435\u043b\u0435\u0444\u043e\u043d\u0430"),r.a.createElement(R.a,{onSubmit:this.sendVerificationQuery.bind(this),className:"login-form"},r.a.createElement(R.a.Item,{validateStatus:this.state.phoneNumberErr?"error":"",help:this.state.phoneNumberErr?this.state.phoneNumberErr:""},r.a.createElement(_.a,{placeholder:"\u041d\u043e\u043c\u0435\u0440 \u0442\u0435\u043b\u0435\u0444\u043e\u043d\u0430",onChange:this.onChangePhone.bind(this)})),r.a.createElement(R.a.Item,null,r.a.createElement(T.a,null,r.a.createElement(j.a,{style:{textAlign:"center"}},r.a.createElement(S.a,{type:"primary",htmlType:"submit",disabled:!this.state.phoneNumber,loading:!!this.state.btnPreloader},"\u041f\u043e\u043b\u0443\u0447\u0438\u0442\u044c SMS \u0441 \u043a\u043e\u0434\u043e\u043c")))))))))}}]),t}(n.Component)),D=function(e){function t(){var e;return Object(v.a)(this,t),(e=Object(E.a)(this,Object(b.a)(t).call(this))).state={defaultAva:""},e}return Object(y.a)(t,e),Object(h.a)(t,[{key:"componentDidMount",value:function(){var e=this.refs.messageText,t=e.innerText;if(t.length>60&&(e.innerText=t.slice(0,60)+" ..."),!this.props.dialog.Photo){var a=this.props.dialog.Title.split(" ",2);a=a[0][0]+a[1][0],this.setState({defaultAva:a})}}},{key:"openDialog",value:function(){this.props.openDialog(this.props.dialog.ConversationId)}},{key:"render",value:function(){var e=this,t={width:"100%",height:"100%",backgroundSize:"cover",backgroundPosition:"center center",backgroundImage:"url(https://testnode1.ymess.org:5000/api/Files/".concat(this.props.dialog.Photo,")"),backgroundRepeat:"no-repeat"};return r.a.createElement(g.c,{to:"/dialog/"+this.props.dialog.ConversationId,onClick:function(){return e.openDialog()}},r.a.createElement("div",{className:"sidebar__item"},r.a.createElement("div",{className:"sidebar__ava"},this.props.dialog.UnreadedCount>0?r.a.createElement("div",{className:"sidebar__badge"},this.props.dialog.UnreadedCount):null,r.a.createElement("div",{className:"sidebar__photo"},this.props.dialog.Photo?r.a.createElement("div",{style:t}):r.a.createElement("div",{className:"sidebar__photo-default"},this.state.defaultAva))),r.a.createElement("div",null,r.a.createElement("div",{className:"sidebar__name"},r.a.createElement("span",null,this.props.dialog.Title)),r.a.createElement("div",{className:"sidebar__message",ref:"messageText"},this.props.dialog.PreviewText))))}}]),t}(n.Component);var q=Object(o.b)(function(e){return{}},function(e){return{openDialog:function(t){return e(f(t))}}})(D);var V=Object(o.b)(function(e){return{dialogs:e.dialogs}},function(e){return{openDialog:function(t){return e(f(t))}}})(function(e){return Object(n.useEffect)(function(){},[]),r.a.createElement("div",{className:"sidebar"},e.dialogs.map(function(e,t){return r.a.createElement(q,{dialog:e,key:e.ConversationId})}))}),F=a(359);var G=Object(o.b)(function(e){return{users:e.users,forwardMessage:e.forwardMessage}},function(e){return{forwardingMessage:function(t){return e(function(e){return{type:"FORWARD_MESSAGE",message:e}}(t))}}})(function(e){var t=Object(n.useState)(!1),a=Object(m.a)(t,2),s=a[0],l=a[1],o=Object(n.useState)(""),c=Object(m.a)(o,2),i=c[0],u=c[1],d=Object(n.useState)(!1),f=Object(m.a)(d,2),v=(f[0],f[1],Object(n.useState)(!1)),h=Object(m.a)(v,2),E=h[0],b=h[1];return Object(n.useEffect)(function(){},[e.user]),Object(n.useEffect)(function(){if(e.message){var t=new Date(e.message.SendingTime),a=new Date;a.setTime(t);var n=a.getHours()+"",r=a.getMinutes()+"";1===n.length&&(n="0"+n),1===r.length&&(r="0"+r),u(n+":"+r)}},[e.message]),E?r.a.createElement(p.a,{to:"/"}):r.a.createElement("div",{className:e.users.Id===e.message.SenderId?"message message-out":"message message-in",onClick:function(){e.forwardingMessage(e.message)},style:e.message&&e.forwardMessage.GlobalId===e.message.GlobalId?{background:"#ebf5ff"}:null},function(t){if(!t.Attachments.length)return t.Text?r.a.createElement("div",{className:"message__text"},t.Text):null;if(2===t.Attachments[0].Type)return r.a.createElement("div",{className:"message__img",onClick:function(){return l(!0)}},r.a.createElement("img",{src:"https://testnode1.ymess.org:5000/api/Files/"+t.Attachments[0].Payload.FileId,alt:""}));if(5===t.Attachments[0].Type){if(!e.users.find(function(e){return e.Id===t.Attachments[0].Payload[0].SenderId})){var a={UsersId:[t.Attachments[0].Payload[0].SenderId],RequestType:28,RequestId:w.makeId(),Type:0};w.send(a)}return r.a.createElement("div",{className:"forwarding_message"},"\u041f\u0435\u0440\u0435\u0441\u043b\u0430\u043d\u043d\u043e\u0435 \u0441\u043e\u043e\u0431\u0449\u0435\u043d\u0438\u0435 \u043e\u0442",r.a.createElement(g.c,{to:"/dialog/"+t.ConversationId},function(t){var a=e.users.find(function(e){return e.Id===t});if(a)return" ".concat(a.NameFirst," ").concat(a.NameSecond)}(t.Attachments[0].Payload[0].SenderId)),r.a.createElement("div",{className:"message__text"},t.Attachments[0].Payload[0].Text))}}(e.message),r.a.createElement("div",{className:"message__time"},i),e.message.Attachments[0]?r.a.createElement(F.a,{visible:s,onCancel:function(){return l(!1)},footer:null,maskClosable:!0},r.a.createElement("img",{className:"popup__img",src:"https://testnode1.ymess.org:5000/api/Files/"+e.message.Attachments[0].Payload.FileId,alt:""})):null,e.message&&e.forwardMessage.GlobalId===e.message.GlobalId?r.a.createElement("div",{className:"message-menu"},r.a.createElement("ul",null,r.a.createElement("li",{onClick:function(){e.onReplyToMessage(e.message)}},"\u041e\u0442\u0432\u0435\u0442\u0438\u0442\u044c"),r.a.createElement("li",{onClick:function(){b(!0)}},"\u041f\u0435\u0440\u0435\u0441\u043b\u0430\u0442\u044c"))):null)});function H(){return(H=Object.assign||function(e){for(var t=1;t<arguments.length;t++){var a=arguments[t];for(var n in a)Object.prototype.hasOwnProperty.call(a,n)&&(e[n]=a[n])}return e}).apply(this,arguments)}function L(e,t){if(null==e)return{};var a,n,r=function(e,t){if(null==e)return{};var a,n,r={},s=Object.keys(e);for(n=0;n<s.length;n++)a=s[n],t.indexOf(a)>=0||(r[a]=e[a]);return r}(e,t);if(Object.getOwnPropertySymbols){var s=Object.getOwnPropertySymbols(e);for(n=0;n<s.length;n++)a=s[n],t.indexOf(a)>=0||Object.prototype.propertyIsEnumerable.call(e,a)&&(r[a]=e[a])}return r}var z=r.a.createElement("g",{transform:"translate(-198)",id:"g69"},r.a.createElement("g",{id:"_x33__22_"},r.a.createElement("g",{id:"g66"},r.a.createElement("path",{id:"path64",d:"m 544.5,99 v 495 c 0,82.021 -66.479,148.5 -148.5,148.5 -82.021,0 -148.5,-66.479 -148.5,-148.5 V 148.5 c 0,-54.673 44.327,-99 99,-99 54.673,0 99,44.327 99,99 V 594 c 0,27.349 -22.176,49.5 -49.5,49.5 -27.349,0 -49.5,-22.151 -49.5,-49.5 V 198 H 297 v 396 c 0,54.673 44.327,99 99,99 54.673,0 99,-44.327 99,-99 V 148.5 C 495,66.479 428.521,0 346.5,0 264.479,0 198,66.479 198,148.5 V 618.75 C 210.202,716.389 295.045,792 396,792 496.955,792 581.798,716.389 594,618.75 V 99 Z"})))),W=function(e){var t=e.svgRef,a=L(e,["svgRef"]);return r.a.createElement("svg",H({xmlSpace:"preserve",viewBox:"0 0 396 792",height:0,width:0,y:"0px",x:"0px",id:"Capa_1",ref:t},a),z)},B=r.a.forwardRef(function(e,t){return r.a.createElement(W,H({svgRef:t},e))});a.p;function J(){return(J=Object.assign||function(e){for(var t=1;t<arguments.length;t++){var a=arguments[t];for(var n in a)Object.prototype.hasOwnProperty.call(a,n)&&(e[n]=a[n])}return e}).apply(this,arguments)}function K(e,t){if(null==e)return{};var a,n,r=function(e,t){if(null==e)return{};var a,n,r={},s=Object.keys(e);for(n=0;n<s.length;n++)a=s[n],t.indexOf(a)>=0||(r[a]=e[a]);return r}(e,t);if(Object.getOwnPropertySymbols){var s=Object.getOwnPropertySymbols(e);for(n=0;n<s.length;n++)a=s[n],t.indexOf(a)>=0||Object.prototype.propertyIsEnumerable.call(e,a)&&(r[a]=e[a])}return r}var Q=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g95"},r.a.createElement("g",{id:"send"},r.a.createElement("polygon",{id:"polygon92",points:"0,38.25 0,216.75 382.5,267.75 0,318.75 0,497.25 535.5,267.75 "}))),X=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g97"}),Z=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g99"}),$=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g101"}),Y=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g103"}),ee=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g105"}),te=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g107"}),ae=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g109"}),ne=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g111"}),re=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g113"}),se=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g115"}),le=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g117"}),oe=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g119"}),ce=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g121"}),ie=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g123"}),ue=r.a.createElement("g",{transform:"translate(0,-38.25)",id:"g125"}),de=function(e){var t=e.svgRef,a=K(e,["svgRef"]);return r.a.createElement("svg",J({xmlSpace:"preserve",viewBox:"0 0 535.5 459",height:0,width:0,y:"0px",x:"0px",id:"Capa_1",ref:t},a),Q,X,Z,$,Y,ee,te,ae,ne,re,se,le,oe,ce,ie,ue)},me=r.a.forwardRef(function(e,t){return r.a.createElement(de,J({svgRef:t},e))});a.p;var fe=Object(o.b)(function(e){return{user:e.users.find(function(e){return!0===e.currentUser}),dialogs:e.dialog,forwardMessage:e.forwardMessage}},function(e){return{}})(function(e){var t=Object(n.useState)(""),a=Object(m.a)(t,2),s=a[0],l=a[1];return Object(n.useEffect)(function(){},[]),r.a.createElement("div",{className:"new-message"},r.a.createElement("label",null,r.a.createElement("input",{type:"file",style:{display:"none"}}),r.a.createElement(B,{className:"new-message__attach",onClick:function(){console.log(55)}})),r.a.createElement("input",{type:"text",className:"new-message__inp",placeholder:"\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u0441\u043e\u043e\u0431\u0449\u0435\u043d\u0438\u0435",value:s,onChange:function(e){l(e.target.value)}}),r.a.createElement(me,{onClick:function(){!function(){if(e.forwardMessage){var t={Messages:[{Id:0,SendingTime:"",SenderId:e.user.Id,ReceiverId:15,ConversationId:null,ConversationType:e.dialog.ConversationType,Read:!1,NodesId:null,ReplyTo:null,Text:s,Attachments:[e.forwardMessage],GlobalId:null}],RequestType:39,RequestId:w.makeId(),Type:0};w.send(t)}else{var a={Messages:[{Id:0,SendingTime:"",SenderId:e.user.Id,ReceiverId:15,ConversationId:null,ConversationType:e.dialog.ConversationType,Read:!1,NodesId:null,ReplyTo:null,Text:s,Attachments:null,GlobalId:null}],RequestType:39,RequestId:w.makeId(),Type:0};w.send(a)}l("")}()},className:"new-message__send"}))});var ge=Object(o.b)(function(e){return{dialogs:e.dialogs,messages:e.messages,forwardMessage:e.forwardMessage}},function(e){return{openDialog:function(t){return e(f(t))}}})(function(e){var t=Object(n.useState)([]),a=Object(m.a)(t,2),s=a[0],l=a[1],o=Object(n.useState)(null),c=Object(m.a)(o,2),i=c[0],u=c[1],d=Object(n.useState)(null),f=Object(m.a)(d,2),g=(f[0],f[1]),p=Object(n.useRef)(null);Object(n.useEffect)(function(){u(e.dialog)},[e.dialog]),Object(n.useEffect)(function(){var t=e.messages.filter(function(t){return t.ConversationId===parseInt(e.match.params.id)});l(t),setTimeout(function(){p.current.scrollTop=45e4},10)},[i,e.match.params.id,e.messages]),Object(n.useEffect)(function(){},[]);var v=function(e){g(e)};return r.a.createElement("div",{className:"dialog"},r.a.createElement("div",{className:"dialog__messages-viewport",ref:p},r.a.createElement("div",{className:"dialog__messages"},i?s.map(function(e){return r.a.createElement(G,{key:e.SendingTime,message:e,onReplyToMessage:v})}):null)),e.forwardMessage?r.a.createElement("div",{className:"reply-message"},"\u041e\u0442\u0432\u0435\u0442 \u043d\u0430:"," ".concat(e.forwardMessage.Text)):null,r.a.createElement(fe,Object.assign({},e,{dialog:i})))});var pe=Object(o.b)(function(e){return{dialogs:e.dialogs,messages:e.messages}},function(e){return{openDialog:function(t){return e(f(t))}}})(function(e){var t=Object(n.useState)([]),a=Object(m.a)(t,2),s=(a[0],a[1]),l=Object(n.useState)(null),o=Object(m.a)(l,2),c=o[0],i=o[1],u=parseInt(e.match.params.id);return Object(n.useEffect)(function(){e.dialogs.map(function(e){e.ConversationId===u&&i(e)})},[e.dialogs]),Object(n.useEffect)(function(){},[e.messages]),Object(n.useEffect)(function(){if(c&&(e.openDialog(u),s([]),!c.messagesRecieved)){var t={ConversationType:c.ConversationType,ConversationId:e.match.params.id,NavigationMessageTime:null,MessageId:null,RequestType:25,RequestId:w.makeId(),Type:0};w.send(t)}},[c]),r.a.createElement(ge,Object.assign({dialog:c},e))}),ve=a(130),he=a.n(ve);function Ee(){return(Ee=Object.assign||function(e){for(var t=1;t<arguments.length;t++){var a=arguments[t];for(var n in a)Object.prototype.hasOwnProperty.call(a,n)&&(e[n]=a[n])}return e}).apply(this,arguments)}function be(e,t){if(null==e)return{};var a,n,r=function(e,t){if(null==e)return{};var a,n,r={},s=Object.keys(e);for(n=0;n<s.length;n++)a=s[n],t.indexOf(a)>=0||(r[a]=e[a]);return r}(e,t);if(Object.getOwnPropertySymbols){var s=Object.getOwnPropertySymbols(e);for(n=0;n<s.length;n++)a=s[n],t.indexOf(a)>=0||Object.prototype.propertyIsEnumerable.call(e,a)&&(r[a]=e[a])}return r}var ye=r.a.createElement("g",null,r.a.createElement("g",null,r.a.createElement("path",{d:"M401.625,210.375v-66.938C401.625,65.025,336.6,0,258.188,0C179.775,0,114.75,65.025,114.75,143.438v66.938 c-32.513,0-57.375,24.862-57.375,57.375v95.625c0,84.15,68.85,153,153,153H306c84.15,0,153-68.85,153-153V267.75 C459,237.15,434.138,210.375,401.625,210.375z M133.875,143.438c0-68.85,55.462-124.312,124.312-124.312 c68.85,0,124.312,55.462,124.312,124.312v66.938h-19.125v-66.938c0-57.375-47.812-105.188-105.188-105.188S153,86.062,153,143.438 v66.938h-19.125V143.438z M344.25,143.438v66.938H172.125v-66.938c0-47.812,38.25-86.062,86.062-86.062 S344.25,95.625,344.25,143.438z M439.875,363.375c0,74.588-59.287,133.875-133.875,133.875h-95.625 c-74.587,0-133.875-59.287-133.875-133.875V267.75c0-21.038,17.212-38.25,38.25-38.25h286.875c21.037,0,38.25,17.212,38.25,38.25 V363.375z"}),r.a.createElement("path",{d:"M258.188,286.875c-26.775,0-47.812,21.037-47.812,47.812c0,15.3,7.65,28.688,19.125,38.25v38.25 c0,15.3,13.388,28.688,28.688,28.688s28.688-13.388,28.688-28.688v-38.25c11.475-9.562,19.125-22.95,19.125-38.25 C306,307.912,284.963,286.875,258.188,286.875z M267.75,361.463v49.725c0,5.737-3.825,9.562-9.562,9.562s-9.562-3.825-9.562-9.562 v-49.725c-11.475-3.825-19.125-15.301-19.125-26.775c0-15.3,13.388-28.688,28.688-28.688s28.688,13.388,28.688,28.688 C286.875,348.075,279.225,357.638,267.75,361.463z"}))),Oe=r.a.createElement("g",null),Te=r.a.createElement("g",null),je=r.a.createElement("g",null),Se=r.a.createElement("g",null),ke=r.a.createElement("g",null),Ie=r.a.createElement("g",null),Ce=r.a.createElement("g",null),Re=r.a.createElement("g",null),_e=r.a.createElement("g",null),Ne=r.a.createElement("g",null),we=r.a.createElement("g",null),Ae=r.a.createElement("g",null),Me=r.a.createElement("g",null),xe=r.a.createElement("g",null),Ue=r.a.createElement("g",null),Pe=function(e){var t=e.svgRef,a=be(e,["svgRef"]);return r.a.createElement("svg",Ee({id:"Capa_1",x:"0px",y:"0px",width:0,height:0,viewBox:"0 0 516.375 516.375",style:{enableBackground:"new 0 0 516.375 516.375"},xmlSpace:"preserve",ref:t},a),ye,Oe,Te,je,Se,ke,Ie,Ce,Re,_e,Ne,we,Ae,Me,xe,Ue)},De=(r.a.forwardRef(function(e,t){return r.a.createElement(Pe,Ee({svgRef:t},e))}),a.p,a(362)),qe=a(357),Ve=a(175),Fe=a.n(Ve);qe.a.locale(Fe.a);var Ge=Object(o.b)(function(e){return{dialogs:e.dialogs,users:e.users}},function(e){return{}})(function(e){var t=Object(n.useState)(null),a=Object(m.a)(t,2),s=a[0],l=a[1],o=Object(n.useState)(""),c=Object(m.a)(o,2),i=c[0],u=c[1],d=Object(n.useState)(null),f=Object(m.a)(d,2),p=f[0],v=f[1],h=Object(n.useState)(document.body.clientWidth),E=Object(m.a)(h,2),b=E[0],y=E[1];return Object(n.useEffect)(function(){},[b]),window.onresize=function(){y(document.body.clientWidth)},Object(n.useEffect)(function(){if(e.users&&s){var t=e.users.find(function(e){return e.Id===s.SecondUid});v(t)}},[e.users]),Object(n.useEffect)(function(){e.dialogs.map(function(e){e.active&&l(e)})},[e.dialogs]),Object(n.useEffect)(function(){if(s){var e={UsersId:[s.SecondUid],RequestType:28,RequestId:w.makeId(),Type:0};if(w.send(e),!s.Photo){var t=s.Title.split(" ",2);t=t[0][0]+t[1][0],u(t)}}},[s]),r.a.createElement("div",{className:"head"},b<=675&&e.location.pathname.indexOf("dialog")>0?r.a.createElement(r.a.Fragment,null,r.a.createElement("div",{className:"head__back"},r.a.createElement(g.b,{to:"/"},"\u041d\u0430\u0437\u0430\u0434"),r.a.createElement("br",null)),r.a.createElement("div",{className:"head__user"},r.a.createElement("div",{className:"head__ava"},s?s.Photo?r.a.createElement("div",{style:{width:"100%",height:"100%",backgroundSize:"cover",backgroundPosition:"center center",backgroundImage:"url(https://testnode1.ymess.org:5000/api/Files/".concat(s.Photo,")"),backgroundRepeat:"no-repeat"}}):r.a.createElement("div",{className:"head__photo-default"},i):null),r.a.createElement("div",null,r.a.createElement("div",{className:"head__name"},s?s.Title:null),r.a.createElement("div",{className:"head__status"},p?r.a.createElement(De.a,{date:1e3*p.Online,locale:"ru"}):null)))):(b<=675&&e.location.pathname.indexOf("dialog"),r.a.createElement(r.a.Fragment,null,r.a.createElement("div",{className:"head__logo"},r.a.createElement("img",{src:he.a})))),r.a.createElement("div",{onClick:function(){var e={Full:!1,AccessToken:M.a.get("AccessToken"),RequestType:34,RequestId:w.makeId(),Type:0};w.send(e),M.a.remove("AccessToken"),M.a.remove("FileAccessToken"),M.a.remove("RefreshToken"),M.a.remove("User"),window.location.replace("/")},className:"head__logout"},"\u0412\u044b\u0445\u043e\u0434"))});var He=Object(o.b)(function(e){return{dialogs:e.dialogs}},function(e){return{}})(function(e){var t=Object(n.useState)(document.body.clientWidth),a=Object(m.a)(t,2),s=a[0],l=a[1];return Object(n.useEffect)(function(){},[]),Object(n.useEffect)(function(){},[s]),window.onresize=function(){l(document.body.clientWidth)},r.a.createElement("div",{className:s>675?"page":"page-mobile"},r.a.createElement(Ge,e),r.a.createElement("div",{className:"page__content"},r.a.createElement(p.b,{exact:!(s>675),path:"/",render:function(e){return r.a.createElement(V,e)}}),r.a.createElement(p.b,{path:"/dialog/:id",render:function(e){return r.a.createElement(pe,e)}})))});var Le=Object(o.b)(function(e){return{dialogs:e.dialogs,users:e.users}},function(e){return{dialogsReceived:function(t){return e(function(e){return{type:"DIALOGS_RECEIVED",dialogs:e}}(t))},openDialog:function(t){return e(f(t))},userReceived:function(t){return e({type:"USER_RECEIVED",users:t})},messagesReceived:function(t){return e(function(e){return{type:"MESSAGES_RECEIVED",messages:e}}(t))}}})(function(e){var t=Object(n.useState)("CHECK_AUTH"),a=Object(m.a)(t,2),s=a[0],l=a[1];return Object(n.useEffect)(function(){if("AUTH_SUCCESS"===s){var e={NavigationMessageTime:null,RequestId:w.makeId(),RequestType:12,Type:0};w.send(e)}},[s]),Object(n.useEffect)(function(){w.wsOnConnect=function(){var t;w.onMessage=function(t){if(console.log(t),0!==t.ErrorCode)console.log(t);else if(6===t.ResponseType){var a={RequestId:w.makeId(),RequestType:27,Type:0};w.send(a),M.a.set("FileAccessToken",t.FileAccessToken),M.a.set("AccessToken",t.Token.AccessToken),M.a.set("RefreshToken",t.Token.RefreshToken),l("AUTH_SUCCESS")}else if(12===t.ResponseType){var n=t.User;n.currentUser=!0,e.userReceived([n])}else 15===t.ResponseType?e.dialogsReceived(t.Conversations):9===t.ResponseType?e.messagesReceived(t.Messages):0===t.ResponseType&&e.userReceived(t.Users)},M.a.get("AccessToken")?(M.a.get("User")?t={Token:{UserId:JSON.parse(M.a.get("User")).Id,AccessToken:M.a.get("AccessToken"),RefreshToken:M.a.get("RefreshToken")},LoginType:0,UidType:1,RequestId:w.makeId(),RequestType:33,Type:0}:M.a.get("Phone")?t={Uid:M.a.get("Phone"),Token:{AccessToken:M.a.get("AccessToken"),RefreshToken:M.a.get("RefreshToken")},LoginType:0,UidType:0,RequestId:w.makeId(),RequestType:33,Type:0}:M.a.get("Email")&&console.log("\u041f\u043e \u043f\u043e\u0447\u0442\u0435"),w.send(t)):l("NO_AUTH")}},[]),r.a.createElement(g.a,null,r.a.createElement(p.b,{path:"/",render:"CHECK_AUTH"===s?function(){return r.a.createElement("div",{className:"preloader test"},r.a.createElement(U.a,{size:"large"}))}:"AUTH_SUCCESS"===s?function(){return r.a.createElement(p.b,{path:"/",component:He})}:function(){return r.a.createElement(p.a,{to:"/auth"})}}),r.a.createElement(p.b,{path:"/auth/by-phone",component:P}),r.a.createElement(p.b,{exact:!0,path:"/auth",component:C}),r.a.createElement(p.b,{exact:!0,path:"/registration",component:x}))});Boolean("localhost"===window.location.hostname||"[::1]"===window.location.hostname||window.location.hostname.match(/^127(?:\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}$/));var ze,We=Object(c.d)(d,ze,Object(c.a)(i.a));l.a.render(r.a.createElement(o.a,{store:We},r.a.createElement(g.a,null,r.a.createElement(Le,null))),document.getElementById("root")),"serviceWorker"in navigator&&navigator.serviceWorker.ready.then(function(e){e.unregister()})}},[[179,1,2]]]);
//# sourceMappingURL=main.d366ec4c.chunk.js.map