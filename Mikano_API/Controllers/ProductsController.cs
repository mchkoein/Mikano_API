using Mikano_API.Helpers;
using Mikano_API.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using WebApi.OutputCache.V2;
using static Mikano_API.Models.KMSEnums;

namespace Mikano_API.Controllers
{
    [Authorize]
    [RoutePrefix("api/Products")]
    public class ProductsController : SharedController<SocketHub>
    {
        private ProductRepository rpstry = new ProductRepository();
        private string kSectionName = "Products";
        private string kActionName = "";

        #region Backend
        [HttpGet]
        public HttpResponseMessage GetAll([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))] DataSourceRequest request)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissions = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.read);
            if (hasPermissions)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    return Request.CreateResponse(HttpStatusCode.OK,
                        rpstry.GetAll().Select(d => new
                        {
                            id = d.id,
                            title = d.title,
                            imgSrc = GetGridImage(d.title, d.imgSrc),
                            category = d.Division.title,
                            capacity = d.PowerCapacity.title,
                            brand = d.Brand.title,
                            subCategory = d.DivisionSubCategory.title,
                            portfolio = d.Portfolio.title,
                            priority = d.priority,
                            isPublished = d.isPublished,
                            dateCreated = d.dateCreated,
                            dateModified = d.dateModified
                        }).ToDataSourceResult(request)
                    );
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetById(int id)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            BrandRepository brandRepository = new BrandRepository();
            PortfolioRepository typeRepo = new PortfolioRepository();
            DivisionRepository categoryRepo = new DivisionRepository();
            PowerCapacityRepository powerCapacityRepo = new PowerCapacityRepository();
            AdministratorRepository adminRpstry = new AdministratorRepository();
            DivisionSubCategoryRepository subDivisionRepository = new DivisionSubCategoryRepository();
            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            var brands = brandRepository.GetAllIsPublished().Select(d => new { id = d.id + "", label = d.title });
            var types = typeRepo.GetAllIsPublished().Select(d => new { id = d.id + "", label = d.title });
            var categories = categoryRepo.GetAllIsPublished().Select(d => new { id = d.id + "", label = d.title });
            var powerCapacities = powerCapacityRepo.GetAllIsPublished().Select(d => new { id = d.id + "", label = d.title });
            var subDivisions = subDivisionRepository.GetAllIsPublished().Select(d => new { id = d.id + "", label = d.Division.title +" - "+ d.title });

            if (hasPermissionsToCreate && id == -1 || hasPermissionsToUpdate && id != -1)
            {
                kActionName = KActions.read.ToString();
                try
                {
                    var entry = rpstry.GetById(id);

                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", entry == null ? kSectionName : entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    if (entry == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                isPublished = "False",
                            },
                            fieldsStatus = new
                            {
                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                types = types,
                                brands = brands,
                                subDivisions = subDivisions,
                                powerCapacities = powerCapacities,
                                categories = categories
                            }

                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            model = new
                            {
                                id = entry.id,
                                title = entry.title,
                                description = entry.description,
                                imgSrc = new UploadController().GetUploadedFiles(entry.imgSrc, null, ProductsDirectory),
                                portfolio_id = entry.portfolio_id + "",
                                brand_id = entry.brand_id + "",
                                category_id = entry.category_id + "",
                                sub_division_id = entry.sub_division_id + "",
                                power_capacity_id = entry.power_capacity_id + "",
                                isPublished = entry.isPublished + "",
                                #region Images
                                Images = new UploadController().GetUploadedFiles(
                                    files: null,
                                    filesArray: entry.ProductMedias.Select(d => new MediaModel
                                    {
                                        mediaSrc = d.imgSrc
                                    }).ToList(),
                                    directory: ProductsDirectory),
                                #endregion

                            },
                            additionalData = new
                            {
                                publishOptions = publishOptions,
                                types = types,
                                brands = brands,
                                subDivisions = subDivisions,
                                powerCapacities = powerCapacities,
                                categories = categories
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, id + "", kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        [HttpPost, HttpPut]
        public HttpResponseMessage Details([ModelBinder(typeof(WebApiDataSourceRequestModelBinder))] DataSourceRequest request, Product entry, SubmissionOptions submissionType, bool inline = false)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissionsToCreate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.create);
            bool hasPermissionsToUpdate = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissionsToCreate || hasPermissionsToUpdate)
            {
                if (entry.id == 0)
                {
                    ModelState.Remove("entry.id");
                }
                if (ModelState.IsValid)
                {
                    try
                    {

                        #region Create
                        if (entry.id == 0 && hasPermissionsToCreate)
                        {
                            kActionName = KActions.create.ToString();
                            #region Manage Images
                            var listOfImages = (entry.Images + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var imagesCounter = 1;
                            foreach (var item in listOfImages)
                            {
                                var imageEntry = new ProductMedia();
                                imageEntry.dateCreated = DateTime.Now;
                                imageEntry.dateModified = DateTime.Now;
                                imageEntry.product_id = entry.id;
                                imageEntry.imgSrc = item;
                                imageEntry.priority = imagesCounter;
                                entry.ProductMedias.Add(imageEntry);
                                imagesCounter++;
                            }
                            #endregion
                            entry.dateCreated = DateTime.Now;
                            entry.dateModified = DateTime.Now;
                            entry.priority = rpstry.GetMaxPriority() + 1;

                            rpstry.Add(entry);
                        }
                        #endregion

                        #region Update
                        else if (hasPermissionsToUpdate)
                        {
                            kActionName = KActions.update.ToString();

                            var oldEntry = rpstry.GetById(entry.id);

                            if (oldEntry == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, entry.id);
                            }
                            #region Manage Images
                            rpstry.DeleteRelatedMedias(oldEntry);
                            var listOfImages = (entry.Images + "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            var imagesCounter = 1;
                            foreach (var item in listOfImages)
                            {
                                var imageEntry = new ProductMedia();
                                imageEntry.dateCreated = DateTime.Now;
                                imageEntry.dateModified = DateTime.Now;
                                imageEntry.product_id = entry.id;
                                imageEntry.imgSrc = item;
                                imageEntry.priority = imagesCounter;
                                oldEntry.ProductMedias.Add(imageEntry);
                                imagesCounter++;
                            }
                            #endregion
                            oldEntry.brand_id = entry.brand_id;
                            oldEntry.title = entry.title;
                            oldEntry.imgSrc = entry.imgSrc;
                            oldEntry.description = entry.description;
                            oldEntry.portfolio_id = entry.portfolio_id;
                            oldEntry.sub_division_id = entry.sub_division_id;
                            oldEntry.category_id = entry.category_id;
                            oldEntry.power_capacity_id = entry.power_capacity_id;
                            oldEntry.isPublished = entry.isPublished;
                            oldEntry.dateModified = DateTime.Now;

                            entry = oldEntry;

                        }
                        #endregion

                        if (submissionType == SubmissionOptions.saveAndPublish)
                        {
                            entry.isPublished = true;
                        }
                        rpstry.Save();

                        if (submissionType == SubmissionOptions.saveAndGoToNext)
                        {
                            entry = rpstry.GetNextEntry(entry);
                        }

                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), entry.title, CollectRequestData(Request, entry), GetIpAddress(Request), false);

                        if (inline)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new[] {
                                new {
                                    id = entry.id,
                                    title = entry.title,
                                    priority = entry.priority,
                                    isPublished = entry.isPublished,
                                    dateCreated = entry.dateCreated,
                                    dateModified = entry.dateModified
                                } }.ToDataSourceResult(request));
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new
                            {
                                id = entry.id,
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, entry.id.ToString(), entry.title, CollectRequestData(Request, entry), GetIpAddress(Request), true);
                        return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = string.Join(",", ModelState.Values.SelectMany(d => d.Errors.Select(r => r.ErrorMessage))) });
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        [HttpPost]
        public string SortGrid(SortGridBindingModel model)
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();
            loggedInUserId = User.Identity.GetUserId();
            bool hasPermissions = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.update);
            if (hasPermissions)
            {
                kActionName = KActions.sort.ToString();
                try
                {
                    var entry = rpstry.GetById(model.id);
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, model.id.ToString(), entry.title, CollectRequestData(Request, model), GetIpAddress(Request), false);
                    var result = rpstry.SortGrid(model.newIndex, model.oldIndex, model.id);

                    return result;
                }
                catch (Exception e)
                {

                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, model.id.ToString(), kSectionName, CollectRequestData(Request, model), GetIpAddress(Request), true);
                    return "failure";
                }
            }
            else
            {
                return "failure";
            }
        }

        [HttpDelete]
        public HttpResponseMessage Delete(string ids = "")
        {
            KMSSectionRepository sectionRpstry = new KMSSectionRepository();
            KMSLogRepository logRpstry = new KMSLogRepository();

            loggedInUserId = User.Identity.GetUserId();

            bool hasPermissions = sectionRpstry.GetPermission(kSectionName, loggedInUserId, (int)KActions.delete);
            if (hasPermissions)
            {
                kActionName = KActions.delete.ToString();
                try
                {
                    Product parentEntry = null;
                    var listOfIds = ids.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parId in listOfIds)
                    {
                        //rpstry.Delete(Convert.ToInt32(parId));
                        var entry = rpstry.GetById(Convert.ToInt32(parId));
                        entry.isDeleted = true;
                        rpstry.Save();
                        logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, parId, entry.title, CollectRequestData(Request, null), GetIpAddress(Request), false);

                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                    });
                }
                catch (Exception e)
                {
                    logRpstry.AddLog(loggedInUserId, User.Identity.Name, kActionName, kSectionName, null, kSectionName, CollectRequestData(Request, null), GetIpAddress(Request), true);
                    return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message);
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }

        [HttpGet]
        public HttpResponseMessage NavigateThroughEntries(int id, string navigation)
        {
            var targetedId = rpstry.GetTargetedEntry(id, navigation);
            return Request.CreateResponse(HttpStatusCode.OK, targetedId);
        }

        #endregion
    }
}