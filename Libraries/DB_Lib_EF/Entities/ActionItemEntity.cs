using Common_Lib;

using DB_Lib_EF.Models.MariaDB;

using Microsoft.EntityFrameworkCore;

namespace DB_Lib_EF.Entities;

public class ActionItemEntity : ActionItem, IDisposable
{
    private bool _disposed = false;

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
            record.UpdateDateTime = DateTime.Now.ToString("yyyy-MM-dd h:mm:ss");
            db.ActionItems.Add(record);
            db.SaveChanges();
            resp.WasSuccess = true;
        }
        catch (DbUpdateException e) // catch specific exception
        {
            resp.Message = $"An error occurred while updating the database. Record: {record}. Error: {e.Message} ::: {e.InnerException}";
        }

        return resp;
    }

    public Response Delete(ActionItem record)
    {
        var resp = new Response();

        try
        {
            using var db   = new TvMaze();
            var       item = db.ActionItems.FirstOrDefault(a => a.Id == record.Id);

            if (item != null)
            {
                db.ActionItems.Remove(item);
                db.SaveChanges();
                resp.WasSuccess = true;
            } else
            {
                resp.Message = $"ActionItem with Id {record.Id} not found";
            }

            return resp;
        }
        catch (DbUpdateException e) // catch specific exception
        {
            resp.Message = $"An error occurred while updating the database. Record: {record}. Error: {e.Message} ::: {e.InnerException}";

            return resp;
        }
    }

    private bool Validate(ActionItem check)
    {
        return !(string.IsNullOrEmpty(check.Message) && string.IsNullOrEmpty(check.Program));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Release managed resources here
            }

            // Release unmanaged resources here

            _disposed = true;
        }
    }

    ~ActionItemEntity()
    {
        Dispose(false);
    }
}

public static class ActionItemModel
{
    public static Response? RecordActionItem(string program, string message, TextFileHandler log)
    {
        using (var entity = new ActionItemEntity())
        {
            var result = entity.Record(new ActionItemEntity() {Program = program, Message = message});

            if (result == null)
            {
                log.Write("Error: ActionItem was not recorded", program, 0);
            } else if (!result.WasSuccess)
            {
                log.Write(result.Message!, program);
            }

            return result;
        }
    }

    public static Response? GetAllActionItems()
    {
        var response = new Response();

        using (var db = new TvMaze())
        {
            response.ResponseObject = db.ActionItems.OrderBy(a => a.UpdateDateTime).ToList();
            response.WasSuccess     = true;
        }

        return response;
    }

    public static Response? DeleteActionItem(ActionItem rec, string program, TextFileHandler log)
    {
        using (var entity = new ActionItemEntity())
        {
            var result = entity.Delete(rec);

            if (result == null)
            {
                log.Write("Error: ActionItem was not recorded", program, 0);
            } else if (!result.WasSuccess)
            {
                log.Write(result.Message!, program);
            }

            return result;
        }
    }
}
