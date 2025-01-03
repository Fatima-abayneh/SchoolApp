﻿using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolApp.Models
{
    public class Announcement
    {
        [Key]
        public int AnnouncementId { get; set; }
        [Display(Name = "Announcement Title")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 20 characters.")]
        [Required]
        public string AnnouncementTitle { get; set; }
        [Display(Name = "Announcement Description")]        
        public string? AnnouncementDescription { get; set; }
        [Display(Name = "Post Date")]
        public DateTime? PostDate { get; set; }
        public string? ImageUrl { get; set; }
        [NotMapped]
        [Display(Name = "Announcement Photo")]
        public IFormFile? ImageFile { get; set; }
        [Display(Name = "Announcement Photo")]
        public string? AnnouncementPhoto { get; set; }       
        public string? AnnouncementDocFile { get; set; }
        [NotMapped]
        [Display(Name = "Announcement File")]
        public IFormFile? AnnouncementFile { get; set; }
        [ForeignKey("AppUser")]
        public string? AnnTearcherId { get; set; }
        public AppUser? AppUser { get; set; }       
    }
}
