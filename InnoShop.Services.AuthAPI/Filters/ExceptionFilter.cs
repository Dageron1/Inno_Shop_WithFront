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

            ResponseDto response;
            HttpStatusCode statusCode;

            if (exception is DbUpdateException dbUpdateException)
            {
                statusCode = HttpStatusCode.InternalServerError;
                response = new ResponseDto
                {
                    Message = "An error occurred while updating the database.",
                };
            }
            else if (exception is DbUpdateConcurrencyException)
            {
                statusCode = HttpStatusCode.Conflict;
                response = new ResponseDto
                {
                    Message = "A concurrency conflict occurred while updating the database.",
                };
            }
            else if (exception is SqlException sqlException)
            {
                statusCode = HttpStatusCode.InternalServerError;
                response = new ResponseDto
                {
                    Message = "A database error occurred.",
                };
            }
            else
            {               
                statusCode = HttpStatusCode.InternalServerError;
                response = new ResponseDto
                {
                    Message = "An unexpected error occurred.",
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
