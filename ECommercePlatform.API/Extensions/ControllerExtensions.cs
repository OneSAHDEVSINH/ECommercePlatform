﻿using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommercePlatform.API.Extensions
{
    public static class ControllerExtensions
    {
        public static Guid? GetCurrentUserId(this ControllerBase controller)
        {
            var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }

        public static string? GetCurrentUserEmail(this ControllerBase controller)
        {
            return controller.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        public static bool IsCurrentUser(this ControllerBase controller, Guid userId)
        {
            var currentUserId = controller.GetCurrentUserId();
            return currentUserId.HasValue && currentUserId.Value == userId;
        }
    }
}