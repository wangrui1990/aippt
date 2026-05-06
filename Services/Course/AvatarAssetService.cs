using AipptAddIn.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AipptAddIn.Services.Course
{
    public static class AvatarAssetService
    {
        private static readonly List<AvatarAssetOption> StaticAvatars = new List<AvatarAssetOption>
        {
            new AvatarAssetOption
            {
                AssetId = "teacher_female",
                DisplayName = "亲切女教师 PNG",
                FileName = "avatar_teacher_female.png",
                Description = "适合常规课堂讲解",
                IsAnimated = false
            },
            new AvatarAssetOption
            {
                AssetId = "teacher_male",
                DisplayName = "年轻男教师 PNG",
                FileName = "avatar_teacher_male.png",
                Description = "适合正式、清爽的课程讲解",
                IsAnimated = false
            },
            new AvatarAssetOption
            {
                AssetId = "cartoon_assistant",
                DisplayName = "卡通教学助手 PNG",
                FileName = "avatar_cartoon_assistant.png",
                Description = "适合小学、科普、兴趣课",
                IsAnimated = false
            },
            new AvatarAssetOption
            {
                AssetId = "science_teacher",
                DisplayName = "科学老师 PNG",
                FileName = "avatar_science_teacher.png",
                Description = "适合理科、实验和科普内容",
                IsAnimated = false
            }
        };

        private static readonly List<AvatarAssetOption> AnimatedAvatars = new List<AvatarAssetOption>
        {
            new AvatarAssetOption
            {
                AssetId = "teacher_nod",
                DisplayName = "亲切教师点头 GIF",
                FileName = "avatar_teacher_nod.gif",
                Description = "轻微点头循环，适合多数课堂讲解",
                IsAnimated = true
            },
            new AvatarAssetOption
            {
                AssetId = "teacher_female_pointer",
                DisplayName = "女教师教杆讲课 GIF",
                FileName = "avatar_teacher_female2.gif",
                Description = "女教师拿教杆讲课的全身形象，适合正式课堂讲解",
                IsAnimated = true
            },
            new AvatarAssetOption
            {
                AssetId = "assistant_wave",
                DisplayName = "卡通助手挥手 GIF",
                FileName = "avatar_assistant_wave.gif",
                Description = "轻快挥手循环，适合开场和互动提示",
                IsAnimated = true
            },
            new AvatarAssetOption
            {
                AssetId = "science_explain",
                DisplayName = "科学老师讲解 GIF",
                FileName = "avatar_science_explain.gif",
                Description = "讲解手势循环，适合理科内容",
                IsAnimated = true
            }
        };

        public static List<AvatarAssetOption> GetOptions(string avatarMode)
        {
            var mode = Normalize(avatarMode);
            if (mode.Contains("静态") || mode.Contains("png"))
            {
                return StaticAvatars.ToList();
            }

            if (mode.Contains("动画") || mode.Contains("gif"))
            {
                return AnimatedAvatars.ToList();
            }

            return new List<AvatarAssetOption>();
        }

        public static string ResolveAvatarPath(NarrationGenerationRequest request)
        {
            if (request == null)
            {
                return string.Empty;
            }

            var mode = Normalize(request.AvatarMode);
            if (mode.Contains("不显示") || mode.Contains("none"))
            {
                return string.Empty;
            }

            if (mode.Contains("本地") || mode.Contains("自定义") || mode.Contains("custom"))
            {
                return File.Exists(request.AvatarPath) ? request.AvatarPath : string.Empty;
            }

            var options = GetOptions(request.AvatarMode);
            var selected = options.FirstOrDefault(item => item.AssetId == request.AvatarAssetId) ?? options.FirstOrDefault();
            if (selected == null)
            {
                return string.Empty;
            }

            return ResolveAssetPath(selected.FileName);
        }

        public static string ResolveAssetPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            foreach (var root in GetAssetRoots())
            {
                var path = Path.Combine(root, fileName);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        private static IEnumerable<string> GetAssetRoots()
        {
            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static", "images", "avatars");
            yield return Path.Combine(Environment.CurrentDirectory, "static", "images", "avatars");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
