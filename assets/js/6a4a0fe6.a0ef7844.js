"use strict";(self.webpackChunkjitterphysics=self.webpackChunkjitterphysics||[]).push([[802],{8385:(e,i,n)=>{n.r(i),n.d(i,{assets:()=>a,contentTitle:()=>o,default:()=>h,frontMatter:()=>r,metadata:()=>t,toc:()=>d});var s=n(4848),l=n(8453);const r={},o="Collision Filters",t={id:"documentation/filters",title:"Collision Filters",description:"There are three types of collision filters in Jitter: world.DynamicTree.Filter, world.BroadPhaseFilter and world.NarrowPhaseFilter.",source:"@site/docs/02_documentation/06-filters.md",sourceDirName:"02_documentation",slug:"/documentation/filters",permalink:"/docs/documentation/filters",draft:!1,unlisted:!1,editUrl:"https://github.com/notgiven688/jitterphysics2/tree/main/docs/docs/02_documentation/06-filters.md",tags:[],version:"current",sidebarPosition:6,frontMatter:{},sidebar:"tutorialSidebar",previous:{title:"Dynamic tree",permalink:"/docs/documentation/dynamictree"},next:{title:"Changelog",permalink:"/docs/changelog"}},a={},d=[{value:"Dynamic tree filter",id:"dynamic-tree-filter",level:2},{value:"Broad phase filter",id:"broad-phase-filter",level:2},{value:"Example: Collision groups",id:"example-collision-groups",level:3},{value:"Narrow phase filter",id:"narrow-phase-filter",level:2}];function c(e){const i={code:"code",h1:"h1",h2:"h2",h3:"h3",header:"header",p:"p",pre:"pre",...(0,l.R)(),...e.components};return(0,s.jsxs)(s.Fragment,{children:[(0,s.jsx)(i.header,{children:(0,s.jsx)(i.h1,{id:"collision-filters",children:"Collision Filters"})}),"\n",(0,s.jsxs)(i.p,{children:["There are three types of collision filters in Jitter: ",(0,s.jsx)(i.code,{children:"world.DynamicTree.Filter"}),", ",(0,s.jsx)(i.code,{children:"world.BroadPhaseFilter"})," and ",(0,s.jsx)(i.code,{children:"world.NarrowPhaseFilter"}),"."]}),"\n",(0,s.jsx)(i.h2,{id:"dynamic-tree-filter",children:"Dynamic tree filter"}),"\n",(0,s.jsxs)(i.p,{children:["The ",(0,s.jsx)(i.code,{children:"world.DynamicTree.Filter"})]}),"\n",(0,s.jsx)(i.pre,{children:(0,s.jsx)(i.code,{className:"language-cs",children:"public Func<IDynamicTreeProxy, IDynamicTreeProxy, bool> Filter { get; set; }\n"})}),"\n",(0,s.jsxs)(i.p,{children:["is the earliest filter applied during a ",(0,s.jsx)(i.code,{children:"world.Step"})," and set by default to ",(0,s.jsx)(i.code,{children:"World.DefaultDynamicTreeFilter"}),":"]}),"\n",(0,s.jsx)(i.pre,{children:(0,s.jsx)(i.code,{className:"language-cs",children:"public static bool DefaultDynamicTreeFilter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)\n{\n    if (proxyA is RigidBodyShape rbsA && proxyB is RigidBodyShape rbsB)\n    {\n        return rbsA.RigidBody != rbsB.RigidBody;\n    }\n\n    return true;\n}\n"})}),"\n",(0,s.jsx)(i.p,{children:"This filters out collisions between shapes that belong to the same body.\nThe dynamic tree will ignore these collisions, and no potential pairs will be created."}),"\n",(0,s.jsxs)(i.p,{children:["For soft bodies, another collision filter is typically used (defined in ",(0,s.jsx)(i.code,{children:"SoftBodies.DynamicTreeCollisionFilter.Filter"}),"), which also filters out collisions between shapes belonging to the same soft body."]}),"\n",(0,s.jsx)(i.h2,{id:"broad-phase-filter",children:"Broad phase filter"}),"\n",(0,s.jsxs)(i.p,{children:["By default ",(0,s.jsx)(i.code,{children:"world.BroadPhaseFilter"})]}),"\n",(0,s.jsx)(i.pre,{children:(0,s.jsx)(i.code,{className:"language-cs",children:"public IBroadPhaseFilter? BroadPhaseFilter { get; set; }\n"})}),"\n",(0,s.jsxs)(i.p,{children:["is ",(0,s.jsx)(i.code,{children:"null"}),". It is used to filter out collisions that passed broad phase collision detection - that is, after the ",(0,s.jsx)(i.code,{children:"DynamicTree"})," has added the collision to the ",(0,s.jsx)(i.code,{children:"PotentialPair"})," hash set."]}),"\n",(0,s.jsxs)(i.p,{children:["This can be useful if custom collision proxies got added to ",(0,s.jsx)(i.code,{children:"world.DynamicTree"}),".\nSince the Jitter ",(0,s.jsx)(i.code,{children:"world"})," only knows how to handle collisions between ",(0,s.jsx)(i.code,{children:"RigidBodyShape"}),"s, a filter must handle the detected collision (i.e. implement custom collision response code and filter out the collision) such that no ",(0,s.jsx)(i.code,{children:"InvalidCollisionTypeException"})," is thrown.\nJitter\u2019s soft body implementation is based on this kind of filter (see ",(0,s.jsx)(i.code,{children:"SoftBodies.BroadPhaseCollisionFilter"}),")."]}),"\n",(0,s.jsx)(i.h3,{id:"example-collision-groups",children:"Example: Collision groups"}),"\n",(0,s.jsx)(i.p,{children:"Collision groups might be easily implemented using a broad phase filter.\nIn this example, there are two 'teams', team blue and team red.\nA filter that disregards all collisions between team members (rigid bodies) of different colors is implemented:"}),"\n",(0,s.jsx)(i.pre,{children:(0,s.jsx)(i.code,{className:"language-cs",children:"public class TeamFilter : IBroadPhaseFilter\n{\n    public class TeamMember { }\n    \n    public static TeamMember TeamRed = new();\n    public static TeamMember TeamBlue = new();\n    \n    public bool Filter(Shape shapeA, Shape shapeB)\n    {\n        if (shapeA.RigidBody.Tag is not TeamMember || shapeB.RigidBody.Tag is not TeamMember)\n        {\n            // Handle collision normally if at least one body is not a member of any team\n            return true;\n        }\n\n        // There is no collision between team red and team blue.\n        return shapeA.RigidBody.Tag == shapeB.RigidBody.Tag;\n    }\n}\n"})}),"\n",(0,s.jsxs)(i.p,{children:["The ",(0,s.jsx)(i.code,{children:"TeamFilter"})," class can then be instantiated and assigned to ",(0,s.jsx)(i.code,{children:"world.BroadPhaseFilter"}),", ensuring that rigid bodies of different colors will not interact:"]}),"\n",(0,s.jsx)(i.pre,{children:(0,s.jsx)(i.code,{className:"language-cs",children:"world.BroadPhaseFilter = new TeamFilter();\n...\nbodyA.Tag = TeamFilter.TeamBlue;\nbodyB.Tag = TeamFilter.TeamRed;\nbodyC.Tag = TeamFilter.TeamRed;\n"})}),"\n",(0,s.jsx)(i.h2,{id:"narrow-phase-filter",children:"Narrow phase filter"}),"\n",(0,s.jsxs)(i.p,{children:["The ",(0,s.jsx)(i.code,{children:"world.NarrowPhaseFilter"})]}),"\n",(0,s.jsx)(i.pre,{children:(0,s.jsx)(i.code,{className:"language-cs",children:"public INarrowPhaseFilter? NarrowPhaseFilter { get; set; }\n"})}),"\n",(0,s.jsx)(i.p,{children:"operates similarly.\nHowever, this callback is called after narrow phase collision detection, meaning detailed collision information (such as normal, penetration depth, and collision points) is available at this stage.\nThe filter can not only exclude collisions but also modify collision information."}),"\n",(0,s.jsxs)(i.p,{children:["The default narrow phase collision filter in Jitter is assigned to an instance of ",(0,s.jsx)(i.code,{children:"TriangleEdgeCollisionFilter"}),", which filters out so-called 'internal edges' for ",(0,s.jsx)(i.code,{children:"TriangleShape"}),"s.\nThese internal edges typically cause collision artifacts when rigid bodies slide over the edges of connected triangles forming static geometry.\nIn the literature, this problem is also known as 'ghost collisions'."]})]})}function h(e={}){const{wrapper:i}={...(0,l.R)(),...e.components};return i?(0,s.jsx)(i,{...e,children:(0,s.jsx)(c,{...e})}):c(e)}},8453:(e,i,n)=>{n.d(i,{R:()=>o,x:()=>t});var s=n(6540);const l={},r=s.createContext(l);function o(e){const i=s.useContext(r);return s.useMemo((function(){return"function"==typeof e?e(i):{...i,...e}}),[i,e])}function t(e){let i;return i=e.disableParentContext?"function"==typeof e.components?e.components(l):e.components||l:o(e.components),s.createElement(r.Provider,{value:i},e.children)}}}]);