(function()
{
 "use strict";
 var Global,SMApp,Web,User,Meaning,Intent,Entity,Interpreter,Resource,NLU,SC$1,CUI,SC$2,ClientExtensions,Client,SC$3,IntelliFactory,Runtime,WebSharper,List,Seq,Random,Arrays,$,console,UI,Doc,Concurrency,Utils,Unchecked,Remoting,AjaxRemotingProvider;
 Global=self;
 SMApp=Global.SMApp=Global.SMApp||{};
 Web=SMApp.Web=SMApp.Web||{};
 User=Web.User=Web.User||{};
 Meaning=Web.Meaning=Web.Meaning||{};
 Intent=Web.Intent=Web.Intent||{};
 Entity=Web.Entity=Web.Entity||{};
 Interpreter=Web.Interpreter=Web.Interpreter||{};
 Resource=Web.Resource=Web.Resource||{};
 NLU=Web.NLU=Web.NLU||{};
 SC$1=Global.StartupCode$SMApp_Web$NLU=Global.StartupCode$SMApp_Web$NLU||{};
 CUI=Web.CUI=Web.CUI||{};
 SC$2=Global.StartupCode$SMApp_Web$CUI=Global.StartupCode$SMApp_Web$CUI||{};
 ClientExtensions=Web.ClientExtensions=Web.ClientExtensions||{};
 Client=Web.Client=Web.Client||{};
 SC$3=Global.StartupCode$SMApp_Web$Client=Global.StartupCode$SMApp_Web$Client||{};
 IntelliFactory=Global.IntelliFactory;
 Runtime=IntelliFactory&&IntelliFactory.Runtime;
 WebSharper=Global.WebSharper;
 List=WebSharper&&WebSharper.List;
 Seq=WebSharper&&WebSharper.Seq;
 Random=WebSharper&&WebSharper.Random;
 Arrays=WebSharper&&WebSharper.Arrays;
 $=Global.jQuery;
 console=Global.console;
 UI=WebSharper&&WebSharper.UI;
 Doc=UI&&UI.Doc;
 Concurrency=WebSharper&&WebSharper.Concurrency;
 Utils=WebSharper&&WebSharper.Utils;
 Unchecked=WebSharper&&WebSharper.Unchecked;
 Remoting=WebSharper&&WebSharper.Remoting;
 AjaxRemotingProvider=Remoting&&Remoting.AjaxRemotingProvider;
 User.New=function(UserName)
 {
  return{
   UserName:UserName
  };
 };
 Meaning=Web.Meaning=Runtime.Class({
  get_TopIntent:function()
  {
   return List.head(List.sortBy(function(i)
   {
    return i.get_Confidence();
   },this.get_Intents()));
  },
  get_Entities:function()
  {
   return(this.get_Unwrap())[1];
  },
  get_Intents:function()
  {
   return(this.get_Unwrap())[0];
  },
  get_Unwrap:function()
  {
   return[this.$0,this.$1];
  }
 },null,Meaning);
 Intent=Web.Intent=Runtime.Class({
  get_Confidence:function()
  {
   return(this.get_Unwrap())[1];
  },
  get_Name:function()
  {
   return(this.get_Unwrap())[0];
  },
  get_Unwrap:function()
  {
   return[this.$0,this.$1];
  }
 },null,Intent);
 Entity=Web.Entity=Runtime.Class({
  get_Value:function()
  {
   return(this.get_Unwrap())[3];
  },
  get_Role:function()
  {
   return(this.get_Unwrap())[2];
  },
  get_Confidence:function()
  {
   return(this.get_Unwrap())[1];
  },
  get_Name:function()
  {
   return(this.get_Unwrap())[0];
  },
  get_Unwrap:function()
  {
   return[this.$0,this.$1,this.$2,this.$3];
  }
 },null,Entity);
 Interpreter=Web.Interpreter=Runtime.Class({
  get_Options:function()
  {
   return(this.get_Unwrap())[1];
  },
  get_Func:function()
  {
   return(this.get_Unwrap())[0];
  },
  get_Unwrap:function()
  {
   return[this.$0[0],this.$0[1]];
  }
 },null,Interpreter);
 Resource.New=function(Name,Description,Url)
 {
  return{
   Name:Name,
   Description:Description,
   Url:Url
  };
 };
 NLU.HelloUser=function(a)
 {
  var $1,a$1;
  return(a$1=NLU.Hello(a),a$1!=null&&a$1.$==1&&(a$1.$0.$==1&&(a$1.$0.$1.$==0&&(a$1.$0.$0.get_Role()==="contact"&&($1=a$1.$0.$0,true)))))?{
   $:1,
   $0:$1
  }:null;
 };
 NLU.Hello=function(a)
 {
  var $1,a$1;
  return a!=null&&a.$==1&&(a$1=NLU.Intent("Hello",a.$0),a$1!=null&&a$1.$==1&&($1=a$1.$0,true))?{
   $:1,
   $0:$1
  }:null;
 };
 NLU.QuickHello=function(a)
 {
  var $1;
  switch(a)
  {
   case"hi":
   case"yo":
   case"hey":
   case"hello":
    return{
     $:0,
     $0:null
    };
   case"help":
    return{
     $:1,
     $0:null
    };
   case"debug on":
    return{
     $:2,
     $0:null
    };
   case"debug off":
    return{
     $:3,
     $0:null
    };
   default:
    return{
     $:4,
     $0:null
    };
  }
 };
 NLU.Intent=function(name,a)
 {
  return a.get_TopIntent().get_Name()===name&&a.get_TopIntent().get_Confidence()>NLU.intentConfidenceThreshold()?{
   $:1,
   $0:List.filter(function(e)
   {
    return e.get_Confidence()>NLU.entityConfidenceThreshold();
   },a.get_Entities())
  }:null;
 };
 NLU.entityConfidenceThreshold=function()
 {
  SC$1.$cctor();
  return SC$1.entityConfidenceThreshold;
 };
 NLU.set_entityConfidenceThreshold=function($1)
 {
  SC$1.$cctor();
  SC$1.entityConfidenceThreshold=$1;
 };
 NLU.intentConfidenceThreshold=function()
 {
  SC$1.$cctor();
  return SC$1.intentConfidenceThreshold;
 };
 NLU.set_intentConfidenceThreshold=function($1)
 {
  SC$1.$cctor();
  SC$1.intentConfidenceThreshold=$1;
 };
 SC$1.$cctor=function()
 {
  SC$1.$cctor=Global.ignore;
  SC$1.intentConfidenceThreshold=0.85;
  SC$1.entityConfidenceThreshold=0.85;
 };
 CUI.helloUserPhrases=function()
 {
  SC$2.$cctor();
  return SC$2.helloUserPhrases;
 };
 CUI.helloPhrases=function()
 {
  SC$2.$cctor();
  return SC$2.helloPhrases;
 };
 CUI.getRandomPhrase=function(phrases)
 {
  return Seq.nth(CUI.rng().Next(0,phrases.get_Length()),phrases);
 };
 CUI.rng=function()
 {
  SC$2.$cctor();
  return SC$2.rng;
 };
 SC$2.$cctor=function()
 {
  SC$2.$cctor=Global.ignore;
  SC$2.rng=new Random.New();
  SC$2.helloPhrases=List.ofArray(["Welcome!","Welcome, my name is Selma.","Welcome to Selma. How can I help?","Hello this is Selma, how can I help?","Hello, I am Selma. How can I help?","Hello, I am Selma. How may I help you now?"]);
  SC$2.helloUserPhrases=List.ofArray(["Hi $user, welcome back.","Welcome $user, nice to see you again..","Hello $user","Good to see you $user."]);
 };
 ClientExtensions.toArray=function(a)
 {
  return Arrays.map(Global.id,$.makeArray(a));
 };
 ClientExtensions.info=function(a)
 {
  console.info(a);
 };
 ClientExtensions.error=function(a)
 {
  $.error(a);
 };
 ClientExtensions["Terminal.Push"]=function(x,i)
 {
  var a,b;
  a=i.get_Func();
  b=i.get_Options();
  x.push(Runtime.CreateFuncWithThis(a),b);
 };
 ClientExtensions["Terminal.Echo'"]=function(x,text)
 {
  x.disable();
  x.echo(text);
  x.enable();
 };
 Client.Term=function()
 {
  var interpreter,options;
  interpreter=Runtime.ThisFunc(function(term,command)
  {
   return((Client.Main().get_Func())(term))(command);
  });
  options=Client.Main().get_Options();
  Global.$("#main").terminal(interpreter,options);
  Client.context().unshift({
   $:0,
   $0:Client.Main()
  });
  return Doc.get_Empty();
 };
 Client.Main=function()
 {
  SC$3.$cctor();
  return SC$3.Main;
 };
 Client.wait=function(f)
 {
  var b;
  Concurrency.Start((b=null,Concurrency.Delay(function()
  {
   f();
   return Concurrency.Zero();
  })),null);
  ClientExtensions["Terminal.Echo'"](Client.currentTerm(),"please wait");
  Client.currentTerm().disable();
  f();
  Client.currentTerm().enable();
 };
 Client.stopSpeaking=function()
 {
  SC$3.$cctor();
  return SC$3.stopSpeaking;
 };
 Client.sayRandom=function(phrases)
 {
  Client.say(CUI.getRandomPhrase(phrases));
 };
 Client.sayVoices=function()
 {
  var voices,i,$1,v;
  voices=ClientExtensions.toArray(Global.speechSynthesis.getVoices());
  Client.say((function($2)
  {
   return function($3)
   {
    return $2("There are currently "+Global.String($3)+" voices installed on this computer or device.");
   };
  }(Global.id))(Arrays.length(voices)));
  for(i=0,$1=Arrays.length(voices)-1;i<=$1;i++){
   v=Arrays.get(voices,i);
   Client.say(((((Runtime.Curried(function($2,$3,$4,$5)
   {
    return $2("Voice "+Global.String($3)+". Name: "+Utils.toSafe($4)+", Local: "+Utils.prettyPrint($5)+".");
   },4))(Global.id))(i))(v.name))(v.localService));
  }
 };
 Client.say=function(text)
 {
  var b;
  Concurrency.Start((b=null,Concurrency.Delay(function()
  {
   var u;
   u=new Global.SpeechSynthesisUtterance(text);
   u.voice=Client.currentVoice();
   Global.speechSynthesis.speak(u);
   return Concurrency.Zero();
  })),null);
  Client.transcribe()?Client.currentTerm().echo(text):void 0;
 };
 Client.initSpeech=function()
 {
  var voices,u,b;
  voices=ClientExtensions.toArray(Global.speechSynthesis.getVoices());
  Arrays.iter(function(v)
  {
   if(Unchecked.Equals(Client.currentVoice(),null)&&(v.name.indexOf("Microsoft Zira")!=-1||v.name.toLowerCase().indexOf("female")!=-1))
    {
     Client.set_currentVoice(v);
     ClientExtensions.info((function($1)
     {
      return function($2)
      {
       return $1("Using voice "+Utils.toSafe($2)+".");
      };
     }(Global.id))(Client.currentVoice().name));
    }
  },voices);
  Unchecked.Equals(Client.currentVoice(),null)&&Arrays.length(voices)>0?(Client.set_currentVoice(Arrays.find(function(v)
  {
   return v["default"];
  },voices)),ClientExtensions.info((function($1)
  {
   return function($2)
   {
    return $1("Using voice "+Utils.toSafe($2)+".");
   };
  }(Global.id))(Client.currentVoice().name)),u=new Global.SpeechSynthesisUtterance(function($1)
  {
   return $1("Using the default speech synthesis voice.");
  }(Global.id)),Concurrency.Start((b=null,Concurrency.Delay(function()
  {
   Global.speechSynthesis.speak(u);
   return Concurrency.Zero();
  })),null)):Unchecked.Equals(Client.currentVoice(),null)?ClientExtensions.error("No speech synthesis voice is available. In order to use Selma you must install a speech synthesis voice on this device or computer."):void 0;
 };
 Client.transcribe=function()
 {
  SC$3.$cctor();
  return SC$3.transcribe;
 };
 Client.set_transcribe=function($1)
 {
  SC$3.$cctor();
  SC$3.transcribe=$1;
 };
 Client.debugMode=function()
 {
  SC$3.$cctor();
  return SC$3.debugMode;
 };
 Client.set_debugMode=function($1)
 {
  SC$3.$cctor();
  SC$3.debugMode=$1;
 };
 Client.currentTerm=function()
 {
  SC$3.$cctor();
  return SC$3.currentTerm;
 };
 Client.set_currentTerm=function($1)
 {
  SC$3.$cctor();
  SC$3.currentTerm=$1;
 };
 Client.currentVoice=function()
 {
  SC$3.$cctor();
  return SC$3.currentVoice;
 };
 Client.set_currentVoice=function($1)
 {
  SC$3.$cctor();
  SC$3.currentVoice=$1;
 };
 Client.context=function()
 {
  SC$3.$cctor();
  return SC$3.context;
 };
 SC$3.$cctor=function()
 {
  var r;
  SC$3.$cctor=Global.ignore;
  function main(term,command)
  {
   var a,b;
   Client.set_currentTerm(term);
   Unchecked.Equals(Client.currentVoice(),null)?Client.initSpeech():void 0;
   a=NLU.QuickHello(command);
   return a.$==1?Client.say("This is the quick help command"):a.$==2?(Client.set_debugMode(true),Client.say("Debug mode is now on.")):a.$==3?(Client.set_debugMode(false),Client.say("Debug mode is now off.")):a.$==4?(ClientExtensions["Terminal.Echo'"](Client.currentTerm(),"please wait"),term.disable(),Concurrency.Start((b=null,Concurrency.Delay(function()
   {
    return Concurrency.Combine(Concurrency.Bind((new AjaxRemotingProvider.New()).Async("SMApp.Web:SMApp.Web.Server.GetMeaning:374767805",[command]),function(a$1)
    {
     var a$2;
     a$2=NLU.HelloUser(a$1);
     return a$2!=null&&a$2.$==1?(Client.say((function($1)
     {
      return function($2)
      {
       return $1("This is the hello intent. The user name is "+Utils.toSafe($2)+".");
      };
     }(Global.id))(a$2.$0.get_Value())),Concurrency.Zero()):(term.echo("This is the whatever intent"),Concurrency.Zero());
    }),Concurrency.Delay(function()
    {
     term.enable();
     return Concurrency.Zero();
    }));
   })),null)):Client.sayRandom(CUI.helloPhrases());
  }
  SC$3.context=[];
  SC$3.currentVoice=null;
  SC$3.currentTerm=null;
  SC$3.debugMode=false;
  SC$3.transcribe=false;
  SC$3.stopSpeaking=Global.speechSynthesis.speaking||Global.speechSynthesis.pending?Global.speechSynthesis.cancel():null;
  SC$3.Main=new Interpreter({
   $:0,
   $0:[function($1)
   {
    return function($2)
    {
     return main($1,$2);
    };
   },(r={},r.name="Main",r.greetings="Welcome to Selma. Type hello to begin or help for more assistance.",r.prompt=">",r)]
  });
 };
}());
