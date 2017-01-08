using System;
using Bala.Raffle;

public partial class _Default : System.Web.UI.Page
{
    RaffleAPILogic logic;

    protected void Page_Load(object sender, EventArgs e)
    {
        logic = new RaffleAPILogic(Request.Params, Response);

        logic.HandlePageLoad();

        //Response
    }

    
}