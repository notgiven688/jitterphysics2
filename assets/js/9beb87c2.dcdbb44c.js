"use strict";(self.webpackChunkjitterphysics=self.webpackChunkjitterphysics||[]).push([[34],{3905:(e,t,n)=>{n.d(t,{Zo:()=>c,kt:()=>g});var i=n(7294);function r(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function a(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);t&&(i=i.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,i)}return n}function l(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?a(Object(n),!0).forEach((function(t){r(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):a(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function o(e,t){if(null==e)return{};var n,i,r=function(e,t){if(null==e)return{};var n,i,r={},a=Object.keys(e);for(i=0;i<a.length;i++)n=a[i],t.indexOf(n)>=0||(r[n]=e[n]);return r}(e,t);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);for(i=0;i<a.length;i++)n=a[i],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(r[n]=e[n])}return r}var d=i.createContext({}),p=function(e){var t=i.useContext(d),n=t;return e&&(n="function"==typeof e?e(t):l(l({},t),e)),n},c=function(e){var t=p(e.components);return i.createElement(d.Provider,{value:t},e.children)},s="mdxType",u={inlineCode:"code",wrapper:function(e){var t=e.children;return i.createElement(i.Fragment,{},t)}},m=i.forwardRef((function(e,t){var n=e.components,r=e.mdxType,a=e.originalType,d=e.parentName,c=o(e,["components","mdxType","originalType","parentName"]),s=p(n),m=r,g=s["".concat(d,".").concat(m)]||s[m]||u[m]||a;return n?i.createElement(g,l(l({ref:t},c),{},{components:n})):i.createElement(g,l({ref:t},c))}));function g(e,t){var n=arguments,r=t&&t.mdxType;if("string"==typeof e||r){var a=n.length,l=new Array(a);l[0]=m;var o={};for(var d in t)hasOwnProperty.call(t,d)&&(o[d]=t[d]);o.originalType=e,o[s]="string"==typeof e?e:r,l[1]=o;for(var p=2;p<a;p++)l[p]=n[p];return i.createElement.apply(null,l)}return i.createElement.apply(null,n)}m.displayName="MDXCreateElement"},1016:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>d,contentTitle:()=>l,default:()=>u,frontMatter:()=>a,metadata:()=>o,toc:()=>p});var i=n(7462),r=(n(7294),n(3905));const a={sidebar_position:5},l="Changelog",o={unversionedId:"changelog",id:"changelog",title:"Changelog",description:"Jitter 2.1.1 (12-17-2023)",source:"@site/docs/changelog.md",sourceDirName:".",slug:"/changelog",permalink:"/docs/changelog",draft:!1,editUrl:"https://github.com/notgiven688/jitterphysics2/tree/main/docs/docs/changelog.md",tags:[],version:"current",sidebarPosition:5,frontMatter:{sidebar_position:5},sidebar:"tutorialSidebar",previous:{title:"Web demo",permalink:"/docs/webasm"}},d={},p=[{value:"Jitter 2.1.1 (12-17-2023)",id:"jitter-211-12-17-2023",level:3},{value:"Jitter 2.1.0 (12-10-2023)",id:"jitter-210-12-10-2023",level:3},{value:"Jitter 2.0.1 (10-28-2023)",id:"jitter-201-10-28-2023",level:3},{value:"Jitter 2.0.0 (10-22-2023)",id:"jitter-200-10-22-2023",level:3},{value:"Jitter 2.0.0-beta (10-17-2023)",id:"jitter-200-beta-10-17-2023",level:3},{value:"Jitter 2.0.0-alpha (09-18-2023)",id:"jitter-200-alpha-09-18-2023",level:3}],c={toc:p},s="wrapper";function u(e){let{components:t,...n}=e;return(0,r.kt)(s,(0,i.Z)({},c,n,{components:t,mdxType:"MDXLayout"}),(0,r.kt)("h1",{id:"changelog"},"Changelog"),(0,r.kt)("h3",{id:"jitter-211-12-17-2023"},"Jitter 2.1.1 (12-17-2023)"),(0,r.kt)("ul",null,(0,r.kt)("li",{parentName:"ul"},"Fixed O(n^2) problem in ",(0,r.kt)("inlineCode",{parentName:"li"},"TriangleMesh")," due to hash collisions."),(0,r.kt)("li",{parentName:"ul"},(0,r.kt)("inlineCode",{parentName:"li"},"WorldBoundingBox")," of ",(0,r.kt)("inlineCode",{parentName:"li"},"Shape")," is now updated even if no ",(0,r.kt)("inlineCode",{parentName:"li"},"RigidBody")," is attached.")),(0,r.kt)("h3",{id:"jitter-210-12-10-2023"},"Jitter 2.1.0 (12-10-2023)"),(0,r.kt)("ul",null,(0,r.kt)("li",{parentName:"ul"},"Added debug drawing for rigid bodies (",(0,r.kt)("inlineCode",{parentName:"li"},"RigidBody.DebugDraw"),")."),(0,r.kt)("li",{parentName:"ul"},"Fixed a bug in ",(0,r.kt)("inlineCode",{parentName:"li"},"CalculateMassInertia")," within ",(0,r.kt)("inlineCode",{parentName:"li"},"TransformedShape.cs"),"."),(0,r.kt)("li",{parentName:"ul"},"Improved raycasting performance and introduced ",(0,r.kt)("inlineCode",{parentName:"li"},"NarrowPhase.PointTest"),"."),(0,r.kt)("li",{parentName:"ul"},(0,r.kt)("strong",{parentName:"li"},"Breaking Change:")," Inverted behavior of ",(0,r.kt)("inlineCode",{parentName:"li"},"BroadPhaseCollisionFilter"),"."),(0,r.kt)("li",{parentName:"ul"},(0,r.kt)("strong",{parentName:"li"},"Breaking Change:")," Inverted definition of damping factors in ",(0,r.kt)("inlineCode",{parentName:"li"},"RigidBody.Damping")," (0 = no damping, 1 = immediate halt)."),(0,r.kt)("li",{parentName:"ul"},"Added ",(0,r.kt)("inlineCode",{parentName:"li"},"RigidBody.SetMassInertia")," overload to enable setting the inverse inertia to zero."),(0,r.kt)("li",{parentName:"ul"},"An exception is now thrown when a body's mass is set to zero."),(0,r.kt)("li",{parentName:"ul"},"Fixed a bug in the friction handling in ",(0,r.kt)("inlineCode",{parentName:"li"},"Contact.cs"),".")),(0,r.kt)("h3",{id:"jitter-201-10-28-2023"},"Jitter 2.0.1 (10-28-2023)"),(0,r.kt)("ul",null,(0,r.kt)("li",{parentName:"ul"},"Fixed a bug in contact initialization which affected soft body physics.")),(0,r.kt)("h3",{id:"jitter-200-10-22-2023"},"Jitter 2.0.0 (10-22-2023)"),(0,r.kt)("p",null,"Initial stable Release."),(0,r.kt)("h3",{id:"jitter-200-beta-10-17-2023"},"Jitter 2.0.0-beta (10-17-2023)"),(0,r.kt)("ul",null,(0,r.kt)("li",{parentName:"ul"},"Added softbodies.")),(0,r.kt)("h3",{id:"jitter-200-alpha-09-18-2023"},"Jitter 2.0.0-alpha (09-18-2023)"),(0,r.kt)("p",null,"Initial Release."))}u.isMDXComponent=!0}}]);