﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Tuhui.Common45.Mvc;
using Tuhui.Common45.Utility;
using Tuhui.Reception.Model;
using Tuhui.Reception.Service;

namespace Tuhui.Reception.WebUI.Controllers
{
    public class ReceptionManagerController : BaseController
    {
        private IReception_UserInfoService _reception_UserInfoService;
        private IReception_ResourceTypeService _reception_ResourceType;
        private IReception_ResourceService _reception_Resource;
        private IImageService _image;
        private IVideoService _video;
        private IReception_ResourceEventService _event;
        private INavigationService _navigation;

        public ReceptionManagerController()
        {
            _reception_UserInfoService = base.InstanceService<Reception_UserInfoService>();
            _reception_ResourceType = base.InstanceService<Reception_ResourceTypeService>();
            _reception_Resource = base.InstanceService<Reception_ResourceService>();
            _image = base.InstanceService<ImageService>();
            _video = base.InstanceService<VideoService>();
            _event = base.InstanceService<Reception_ResourceEventService>();
            _navigation = base.InstanceService<NavigationService>();
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult LogOn()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "ReceptionManager");
            }
            return View();
        }

        [HttpPost]
        public ActionResult LogOn(Reception_UserInfo model, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("", "登录名不能为空");
                return View(model);
            }
            if (string.IsNullOrWhiteSpace(model.Pwd))
            {
                ModelState.AddModelError("", "密码不能为空");
                return View(model);
            }
            // 如果验证不通过
            if (!_reception_UserInfoService.ValidationUser(model))
            {

                ModelState.AddModelError("", "用户名密码不正确");
                return View(model);
            }
            FormsAuthentication.SetAuthCookie(model.Name, false);

            var _user = _reception_UserInfoService.GetUserByUserName(model.Name);

            SessionHelper.LogOnUser<Reception_UserInfo>(_user);
            
            return RedirectToAction("Index", "ReceptionManager");
        }

        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            return RedirectToAction("LogOn", "ReceptionManager");
        }

        public ActionResult ResourceType()
        {
            return View();
        }

        //获取资源列表
        public ActionResult GetResourceTypeList()
        {
            var list = _reception_ResourceType.GetList();

            return Json(list,JsonRequestBehavior.AllowGet);
        }

        public ActionResult ResourceTypeModify(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                @ViewBag.TitleName = "资源分类管理 -> 添加页面";
                return View(new Reception_ResourceType());
            }
            else
            {
                @ViewBag.TitleName = "资源分类管理 -> 编辑页面";
                Reception_ResourceType entity = _reception_ResourceType.Get(id);

                return View(entity);
            }
        }

        [HttpPost]
        public ActionResult ResourceTypeModify(Reception_ResourceType model)
        {
            if (string.IsNullOrEmpty(model.RT_ID))
            {
                model.RT_ID = CommonFun.GenerGuid();
                model.ImgPath = "";
                model.CreateTime = DateTime.Now;
                //添加资源分类
                _reception_ResourceType.Insert(model);
            }
            else {
                //修改资源分类
                _reception_ResourceType.Update(model);
            }

            return RedirectToAction("ResourceType");
        }

        //删除资源分类
        public ActionResult ResourceTypeDelete(string id) 
        {
            if (string.IsNullOrEmpty(id))
            {
                //抛出异常
            }
            int i = _reception_ResourceType.Delete(id);

            return Json(true);
        }

        public ActionResult Resource()
        {
            return View();
        }

        //资源列表
        public ActionResult GetResourcePageList(string Name,string SSJD,string RStatus,string RT_ID,int pageIndex = 1, int pageSize = 10)
        {
            Reception_Resource entity = new Reception_Resource
            {
                Name = Name,
                SSJD = SSJD,
                RStatus = RStatus,
                RT_ID = RT_ID
            };
            var list = _reception_Resource.GetPageList(entity, pageIndex, pageSize);

            //处理
            if (list.PageData.Count > 0) {
                foreach (var item in list.PageData) {
                    //项目状态
                    switch (item.RStatus)
                    { 
                        case "1":
                            item.RStatus = "未开工";
                            break;
                        case "2":
                            item.RStatus = "已开工";
                            break;
                        case "3":
                            item.RStatus = "已投产";
                            break;
                        default:
                            item.RStatus = "";
                            break;
                    }
                }
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetResourceList()
        {
            var list = _reception_Resource.GetList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ResourceModify(string id)
        {
            List<Reception_ResourceType> resourceTypeList = _reception_ResourceType.GetList();
            resourceTypeList.Insert(0, new Reception_ResourceType { 
                RT_ID = "",
                Name = "--请选择--"
            });
            ViewData["resourceTypeList"] = new SelectList(resourceTypeList, "RT_ID", "Name");

            if (string.IsNullOrEmpty(id))
            {
                @ViewBag.TitleName = "资源管理 -> 添加页面";
                Reception_Resource entity = new Reception_Resource();
                entity.StartTime = DateTime.Now;
                entity.EndTime = DateTime.Now;

                return View(entity);
            }
            else
            {
                @ViewBag.TitleName = "资源管理 -> 编辑页面";
                Reception_Resource entity = _reception_Resource.Get(id);

                return View(entity);
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ResourceModify(Reception_Resource model, string imageList,string videoList)
        {
            
            if (string.IsNullOrEmpty(model.R_ID))
            {
                model.R_ID = CommonFun.GenerGuid();
                model.AddTime = DateTime.Now;
                //添加资源
                _reception_Resource.Insert(model);
            }
            else
            {
                //修改资源
                _reception_Resource.Update(model);
            }

            //图片处理

            //删除图片
            _image.DeleteList(model.R_ID);

            if (!string.IsNullOrEmpty(imageList)) 
            {
                string[] imageArray = imageList.Split('|');

                //添加图片
                foreach (var item in imageArray)
                {
                    _image.Insert(new Image { 
                       I_ID = CommonFun.GenerGuid(),
                       AddTime = DateTime.Now,
                       Obj_ID = model.R_ID,
                       ImagePath = item,
                       ImageSource = "1"
                    });
                }
            }

            //视频处理
            _video.DeleteList(model.R_ID);

            if (!string.IsNullOrEmpty(videoList))
            {
                string[] videoArray = videoList.Split('|');

                //添加图片
                foreach (var item in videoArray)
                {
                    _video.Insert(new Video
                    {
                        V_ID = CommonFun.GenerGuid(),
                        AddTime = DateTime.Now,
                        Obj_ID = model.R_ID,
                        VideoPath = item,
                        VideoSource = "1"
                    });
                }
            }

            return RedirectToAction("Resource");
        }

        //删除资源分类
        public ActionResult ResourceDelete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                //抛出异常
            }
            //删除资源
            _reception_Resource.Delete(id);
            //删除资源图片
            _image.DeleteList(id);
            //删除资源视频
            _video.DeleteList(id);

            return Json(true);
        }

        public ActionResult JTMap()
        {
            return View();
        }

        public ActionResult ResourceEvent()
        {
            return View();
        }

        public ActionResult Navigation()
        {
            return View();
        }

        public ActionResult GetResourceEventPageList(string Name,string R_ID,int pageIndex = 1,int pageSize = 10)
        {
            Reception_ResourceEvent entity = new Reception_ResourceEvent
            {
                Name = Name,
                R_ID = R_ID
            };
            var list = _event.GetPageList(entity, pageIndex, pageSize);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        //删除大事件
        public ActionResult ResourceEventDelete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                //抛出异常
            }
            _event.Delete(id);
           
            return Json(true);
        }

        public ActionResult ResourceEventModify(string id)
        {
            List<Reception_Resource> resourceEventList = _reception_Resource.GetList();
            resourceEventList.Insert(0, new Reception_Resource
            {
                R_ID = "",
                Name = "--请选择--"
            });
            ViewData["resourceEventList"] = new SelectList(resourceEventList, "R_ID", "Name");

            if (string.IsNullOrEmpty(id))
            {
                @ViewBag.TitleName = "大事件管理 -> 添加页面";

                return View(new Reception_ResourceEvent());
            }
            else
            {
                @ViewBag.TitleName = "大事件管理 -> 编辑页面";
                Reception_ResourceEvent entity = _event.Get(id);

                return View(entity);
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ResourceEventModify(Reception_ResourceEvent model)
        {
            if (string.IsNullOrEmpty(model.RE_ID))
            {
                model.RE_ID = CommonFun.GenerGuid();
                model.AddTime = DateTime.Now;
                //添加大事件
                _event.Insert(model);
            }
            else
            {
                //修改大事件
                _event.Update(model);
            }

            return RedirectToAction("ResourceEvent");
        }

        //获取图片列表
        public ActionResult ResourceImageList(string id)
        {
            var list = _image.GetList(id);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        //获取视频列表
        public ActionResult ResourceVideoList(string id)
        {
            var list = _video.GetList(id);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        //获取导航分页列表
        public ActionResult GetNavigationPageList(string Name, int pageIndex = 1, int pageSize = 10)
        {
            Navigation entity = new Navigation
            {
                Name = Name
            };
            var list = _navigation.GetPageList(entity, pageIndex, pageSize);

            return Json(list, JsonRequestBehavior.AllowGet);        
        }

	}
}