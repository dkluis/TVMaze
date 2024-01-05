using DB_Lib_EF.Models.MariaDB;

using Microsoft.EntityFrameworkCore;

namespace DB_Lib_EF.Entities;

public class ActionItemEntity : ActionItem
{
    public Response Record(ActionItem record)
    {
        var resp = new Response();

        if (!Validate(record))
        {
            resp.Message = "Message or Program or both were blank";

            return resp;
        }

        try
        {
            using var db = new TvMaze();
            record.UpdateDateTime = DateTime.Now.ToString("yyyy-MM-dd");
            db.ActionItems.Add(record);
            db.SaveChanges();
            resp.WasSuccess = true;
        }
        catch (DbUpdateException e) // catch specific exception
        {
            resp.Message = $"An error occurred while updating the database. Record: {record}. Error: {e.Message} {e.InnerException}";
        }

        return resp;
    }

    private bool Validate(ActionItem check)
    {
        return !(string.IsNullOrEmpty(check.Message) && string.IsNullOrEmpty(check.Program));
    }
}
