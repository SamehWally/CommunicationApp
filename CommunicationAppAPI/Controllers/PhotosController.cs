using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CommunicationAppApi.Data;
using CommunicationAppApi.Dtos;
using CommunicationAppApi.Helpers;
using CommunicationAppApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Principal;

namespace CommunicationAppApi.Controllers
{
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly ICommunicationRepository _repo;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private readonly IMapper _mapper;
        private Cloudinary _cloudinary;

        public PhotosController(ICommunicationRepository repo, IOptions<CloudinarySettings> cloudinaryConfig,
        IMapper mapper)
        {
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;
            _repo = repo;
            Account acc = new Account(
               _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);

        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepository = await _repo.GetPhoto(id);
            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepository);
            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm] PhotoForCreateDto photoForCreateDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var userFromRepo = await _repo.GetUser(userId, true);
            var file = photoForCreateDto.File;
            var uploadResult = new ImageUploadResult();
            if (file != null && file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation()
                       .Width(500).Height(500).Crop("fill").Gravity("face")
                    };
                    uploadResult = _cloudinary.Upload(uploadParams);
                }

            }
            photoForCreateDto.Url = uploadResult.Uri.ToString();
            photoForCreateDto.publicId = uploadResult.PublicId;
            var photo = _mapper.Map<Photo>(photoForCreateDto);
            if (!userFromRepo.Photos.Any(p => p.IsMain))
                photo.IsMain = true;
            userFromRepo.Photos.Add(photo);
            if (await _repo.SaveAll())
            {
                var PhotoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new { id = photo.Id }, PhotoToReturn);
            }

            return BadRequest("خطأ في إضافة الصورة");

        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var userFromRepo = await _repo.GetUser(userId, true);
            if (!userFromRepo.Photos.Any(p => p.Id == id))
                return Unauthorized();
            var DesiredMainPhoto = await _repo.GetPhoto(id);
            if (DesiredMainPhoto.IsMain)
                return BadRequest("هذه هي الصورة الأساسية بالفعل");
            var CurrentMainPhoto = await _repo.GetMainPhotoForUser(userId);
            CurrentMainPhoto.IsMain = false;
            DesiredMainPhoto.IsMain = true;
            if (await _repo.SaveAll())
                return NoContent();
            return BadRequest("لايمكن تعديل الصورة الأساسية");

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            var userFromRepo = await _repo.GetUser(userId, true);
            if (!userFromRepo.Photos.Any(p => p.Id == id))
                return Unauthorized();
            var Photo = await _repo.GetPhoto(id);
            if (Photo.IsMain)
                return BadRequest("لايمكن حذف الصورة الأساسية");
            if (Photo.PublicId != null)
            {
                var deleteParams = new DeletionParams(Photo.PublicId);
                var result = this._cloudinary.Destroy(deleteParams);
                if (result.Result == "ok")
                {
                    _repo.Delete(Photo);
                }
            }
            if (Photo.PublicId == null)
            {
                _repo.Delete(Photo);
            }

            if (await _repo.SaveAll())
                return Ok();
            return BadRequest("فشل حذف الصورة");

        }
    }
}
