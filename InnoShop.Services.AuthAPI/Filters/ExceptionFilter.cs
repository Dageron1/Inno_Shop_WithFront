using InnoShop.Services.AuthAPI.Models.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;

namespace InnoShop.Services.AuthAPI.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            ResponseDto<AuthServiceResult> response;
            HttpStatusCode statusCode;

            if (exception is DbUpdateException dbUpdateException)
            {
                statusCode = HttpStatusCode.InternalServerError;
                response = new ResponseDto<AuthServiceResult>
                {
                    IsSuccess = false,
                    Message = "An error occurred while updating the database.",
                    Errors = new List<string> { dbUpdateException.InnerException?.Message ?? dbUpdateException.Message }
                };
            }
            else if (exception is DbUpdateConcurrencyException)
            {
                statusCode = HttpStatusCode.Conflict;
                response = new ResponseDto<AuthServiceResult>
                {
                    IsSuccess = false,
                    Message = "A concurrency conflict occurred while updating the database.",
                };
            }
            else if (exception is SqlException sqlException)
            {
                statusCode = HttpStatusCode.InternalServerError;
                response = new ResponseDto<AuthServiceResult>
                {
                    IsSuccess = false,
                    Message = "A database error occurred.",
                    Errors = new List<string> { sqlException.Message }
                };
            }
            else
            {               
                statusCode = HttpStatusCode.InternalServerError;
                response = new ResponseDto<AuthServiceResult>
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred.",
                    Errors = new List<string> { exception.Message }
                };
            }

            context.Result = new ObjectResult(response)
            {
                StatusCode = (int)statusCode
            };

            context.ExceptionHandled = true;
        }
    }
}
