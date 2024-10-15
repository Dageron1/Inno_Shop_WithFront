using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace InnoShop.Services.ProductAPI.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            HttpStatusCode statusCode;
            var message = string.Empty;
            
            if (exception is DbUpdateException dbUpdateException)
            {
                statusCode = HttpStatusCode.InternalServerError;
                message = $"An error occurred while updating the database: {dbUpdateException.InnerException?.Message ?? dbUpdateException.Message}";
            }
            else if (exception is DbUpdateConcurrencyException)
            {
                statusCode = HttpStatusCode.Conflict;
                message = "A concurrency conflict occurred while updating the database.";
            }
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = HttpStatusCode.Forbidden;
                message = "You do not have permission to update this product.";
            }
            else if (exception is SqlException sqlException)
            {
                statusCode = HttpStatusCode.InternalServerError;
                message = $"A database error occurred: {sqlException.Message}";
            }
            else
            {
                statusCode = HttpStatusCode.InternalServerError;
                message = $"An unexpected error occurred: {exception.Message}";
            }

            context.Result = new ObjectResult(message)
            {
                StatusCode = (int)statusCode
            };

            context.ExceptionHandled = true;
        }
    }
}
