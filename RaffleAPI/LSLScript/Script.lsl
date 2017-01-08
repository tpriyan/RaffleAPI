//  global variables
integer channelDialog;
key     toucherID;
key ownerId;
integer      listenId;
list    choicesActive = ["Enroll", "Status", "Exit"];
list    choicesActiveAdmin = ["Draw", "Close raffle","Enroll", "Exit"];

list    choicesInActive = ["Exit"];
list    choicesInActiveAdmin = ["Start raffle", "Exit"];

list    choicesActiveClosed = ["Exit"];
list    choicesInActiveClosedAdmin = ["Close raffle","Draw","Exit"];
string  message = "\nPlease make a choice.";
integer a = 0;
key http_enroll_req_id;
string avatarName;

key requestId_checkActive;
key requestId_startRaffle;
key requestId_enroll;
key requestId_check;
key requestId_drawRaffle;
key requestId_closeRaffle;

string x;
integer  gListener;
integer Durationchannel = -13572468;

default
{
    state_entry()
    {
        
    }

    touch_start(integer total_number)
    {
        // The channel computation can go in state_entry() or similar
        channelDialog = -1 - (integer)("0x" + llGetSubString( (string) llGetKey(), -7, -1) );
        
        // llTextBox(llDetectedKey(0), "Enter the duration for raffle", Durationchannel);
        toucherID = llDetectedKey(0);
        avatarName = llDetectedName(0);
        ownerId = llGetOwner();

       // llSay(0, "http://desihomes.zapto.org:8080/raffleapi.aspx?action=isactive&&objectguid=" + (string) llGetKey());
        // Check if the object is active
        requestId_checkActive = llHTTPRequest("http://desihomes.zapto.org:8080/raffleapi.aspx?action=isactive&&objectguid=" + (string) llGetKey(), [], "");
    }
    
    listen(integer channel, string name, key id, string message)
    {
        
       
        
        if(channel == Durationchannel)
        {
             llListenRemove(gListener);  
            requestId_startRaffle = llHTTPRequest("http://desihomes.zapto.org:8080/raffleapi.aspx?action=startnewdraw&&objectguid=" + (string) llGetKey() + "&duration=" + message, [], "");
               // x = "http://desihomes.zapto.org:8080/raffleapi.aspx?action=startnewdraw&&objectguid=" + (string) llGetKey();
             //   llSay(0, x);
        }

        if(channel == channelDialog)
        {
            llListenRemove(listenId);  
            if(message == "Exit")
            {
                llSay(0, "Bye!");
            }
            else if(message == "Start raffle")
            {
                // llSay(0, "start");
                 llTextBox(toucherID, "Enter the duration for raffle", Durationchannel);
                gListener = llListen(Durationchannel, "", "", "");     
                
                
                
            }
            else if(message == "Enroll")
            {
                requestId_enroll = llHTTPRequest("http://desihomes.zapto.org:8080/raffleapi.aspx?action=add&&objectguid=" + (string)  llGetKey() + "&name=" + avatarName, [], "");
              //  llSay(0, "http://desihomes.zapto.org:8080/raffleapi.aspx?action=add&&objectguid=" + (string)  llGetKey() + "&name=" + avatarName);
            }
            else if(message == "Status")
            {
                requestId_check =  llHTTPRequest("http://desihomes.zapto.org:8080/raffleapi.aspx?action=check&&objectguid=" + (string)  llGetKey() + "&name=" + avatarName, [], "");
               //  llSay(0, "http://desihomes.zapto.org:8080/raffleapi.aspx?action=check&&objectguid=" + (string)  llGetKey() + "&name=" + avatarName);
            }
            else if(message == "Draw")
            {
                requestId_drawRaffle = llHTTPRequest("http://desihomes.zapto.org:8080/raffleapi.aspx?action=draw&&objectguid=" + (string)  llGetKey(), [], "");
                // llSay(0, "http://desihomes.zapto.org:8080/raffleapi.aspx?action=draw&&objectguid=" + (string)  llGetKey());
            }
            else if(message == "Close raffle")
            {
                requestId_closeRaffle = requestId_drawRaffle = llHTTPRequest("http://desihomes.zapto.org:8080/raffleapi.aspx?action=close&&objectguid=" + (string)  llGetKey(), [], "");
                // llSay(0, "http://desihomes.zapto.org:8080/raffleapi.aspx?action=close&&objectguid=" + (string)  llGetKey());
            }
        }            
    }
    http_response(key request_id, integer status, list metadata, string body)
    {
        
       // llSay(0,body);
        // Handle close raffle
        if(request_id == requestId_closeRaffle)
        {
            if(body == "CLOSED")
            {
                llSay(0, "The raffle is closed");
            }
        }
        
        // Handle start raffle
        else if(request_id == requestId_startRaffle)
        {
            if(body == "NEWRAFFLESTARTED")
            {
                llSay(0, "New raffle started");
            }
        }
        
        // Handle enroll
        else if(request_id == requestId_enroll)
        {
            if(body == "ADDED")
            {
                llSay(0, avatarName + " has been added!");
            }
            else if(body == "ALREADYEXISTS")
            {
                 llSay(0, "You are already in raffle");
            }
        }        
        
        // Handle check
        else if(request_id == requestId_check)
        {
            if(body == "ALREADYEXISTS")
            {
                 llSay(0, "You are already in raffle");
            }
            else if(body == "DOESNOTEXISTS")
            {
                llSay(0, "You are already in raffle");
            }
        }
        
        // Handle draw
        else if(request_id == requestId_drawRaffle)
        {
            if(body == "$$$")
            {
                 llSay(0, "Oh no, we have no winners!");
            }
            else
            {
                llSay(0, "The winner is " + body);
            }
        }
        
        // Handle display of dialog
       else if(request_id == requestId_checkActive)
        {
            if(body == "INACTIVE")
            {
                if(toucherID == ownerId)
                {
                    llDialog(toucherID, message, choicesInActiveAdmin, channelDialog);
                }
                else
                {
                    llDialog(toucherID, "\nThe raffle is inactive", choicesInActive, channelDialog);
                }
            }
            else if(body == "ACTIVE")
            {
                if(toucherID == ownerId)
                {
                    llDialog(toucherID, message, choicesActiveAdmin, channelDialog);
                }
                else
                {
                    llDialog(toucherID, message, choicesActive, channelDialog);
                }
            }
            else if(body == "ACTIVEBUTCLOSED")
            {
                if(toucherID == ownerId)
                {
                    llDialog(toucherID, "The raffle has ended", choicesInActiveClosedAdmin, channelDialog);
                }
                else
                {
                    llDialog(toucherID, "Sorry! The raffle is closed for entries", choicesActiveClosed, channelDialog);
                }
            }
            listenId = llListen(channelDialog, "", toucherID, "");
        }
    }

}

