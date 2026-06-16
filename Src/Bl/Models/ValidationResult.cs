using System;
using System.Collections.Generic;
using System.Text;

namespace SharedKernel.Bl.Models
{
    public class EntityValidationResult
    {

        public EntityValidationResult(bool success, string msg)
        {
            this.Success = success;
            this.Message = msg;
        }

        public bool Success { get; set; } = false;
        public string Message { get; set; } = null!;
    }
}
