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

            switch (exception)
            {
                case DbUpdateConcurrencyException:
                    statusCode = HttpStatusCode.Conflict;
                    message = "A concurrency conflict occurred while updating the database.";
                    break;
                case DbUpdateException dbUpdateException:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = $"An error occurred while updating the database: {dbUpdateException.InnerException?.Message ?? dbUpdateException.Message}";
                    break;
                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Forbidden;
                    message = "You do not have permission to update this product.";
                    break;
                case SqlException sqlException:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = $"A database error occurred: {sqlException.Message}";
                    break; 
                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = $"An unexpected error occurred: {exception.Message}";
                    break;
            }

            context.Result = new ObjectResult(message)
            {
                StatusCode = (int)statusCode
            };

            context.ExceptionHandled = true;
        }
    }
}
