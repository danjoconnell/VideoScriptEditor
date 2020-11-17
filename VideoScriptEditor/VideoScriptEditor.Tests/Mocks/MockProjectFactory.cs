using System.Collections.Generic;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Cropping;
using VideoScriptEditor.Models.Masking.Shapes;
using VideoScriptEditor.Models.Primitives;

namespace VideoScriptEditor.Tests.Mocks
{
    public static class MockProjectFactory
    {
        public static ProjectModel CreateMockCroppingProject()
        {
            ProjectModel project = new ProjectModel()
            {
                ScriptFileSource = @"TestFiles\AVSSourceTestScript-640x480-29.97fps.avs"
            };
            project.Cropping.CropSegments.Add(
                new CropSegmentModel(0, 82, 0,
                    new KeyFrameModelCollection()
                    {
                        new CropKeyFrameModel(0, 92.50079239302693, 0, 202.44690966719497, 480, 0),
                        new CropKeyFrameModel(32, 65.72424722662441, 0, 229.2234548335975, 480, 0),
                        new CropKeyFrameModel(52, 25.559429477020615, 0, 269.3882725832013, 480, 0),
                        new CropKeyFrameModel(67, 12.17115689381933, 0, 282.77654516640257, 480, 0),
                        new CropKeyFrameModel(70, 22.516640253565754, 10.710618066560983, 272.43106180665615, 469.289381933439, 0),
                        new CropKeyFrameModel(82, 33.470681458003185, 23.125198098256703, 261.4770206022187, 456.8748019017433, 0)
                    },
                    "Crop 1"
                )
            );
            project.Cropping.CropSegments.Add(
                new CropSegmentModel(122, 268, 0,
                    new KeyFrameModelCollection()
                    {
                        new CropKeyFrameModel(122, 0, 92, 28, 388, 0),
                        new CropKeyFrameModel(127, 0, 92, 94, 388, 0),
                        new CropKeyFrameModel(139, 0, 92, 140, 388, 0),
                        new CropKeyFrameModel(152, 0, 92, 162, 388, 0),
                        new CropKeyFrameModel(167, 0, 92, 133, 388, 0),
                        new CropKeyFrameModel(216, 0, 66.64300453982673, 133, 413.35699546017327, 0),
                        new CropKeyFrameModel(250, 0, 45.24803962030552, 133, 434.7519603796945, 0),
                        new CropKeyFrameModel(255, 0, 45.24803962030552, 133, 434.7519603796945, 0),
                        new CropKeyFrameModel(268, 0, 34.15435410647973, 47.42014032191505, 445.84564589352027, 0)
                    },
                    "Crop 2"
                )
            );
            project.Cropping.CropSegments.Add(
                new CropSegmentModel(107, 299, 1,
                    new KeyFrameModelCollection()
                    {
                        new CropKeyFrameModel(107, 0, 92, 71.08460678345135, 388, 0),
                        new CropKeyFrameModel(114, 0, 92, 188.80357808423386, 388, 0),
                        new CropKeyFrameModel(117, 28, 92, 211.25456578456948, 388, 0),
                        new CropKeyFrameModel(122, 28, 92, 211.25456578456948, 388, 0),
                        new CropKeyFrameModel(127, 94, 92, 145.25456578456948, 388, 0),
                        new CropKeyFrameModel(139, 140, 92, 99.25456578456948, 388, 0),
                        new CropKeyFrameModel(152, 162, 206.43356643356645, 123.40841193841561, 273.56643356643355, 0),
                        new CropKeyFrameModel(167, 133, 206.43356643356645, 152.4084119384156, 273.56643356643355, 0),
                        new CropKeyFrameModel(184, 133, 291.1888111888111, 188.4923280223316, 188.8111888111889, 0),
                        new CropKeyFrameModel(202, 133, 291.1888111888111, 235.2442884020263, 188.8111888111889, 0),
                        new CropKeyFrameModel(217, 133, 252.3609118904207, 235.2442884020263, 227.6390881095793, 0),
                        new CropKeyFrameModel(232, 133, 252.3609118904207, 235.2442884020263, 227.6390881095793, 0),
                        new CropKeyFrameModel(241, 133, 252.3609118904207, 213.8493234825051, 227.6390881095793, 0),
                        new CropKeyFrameModel(255, 133, 225.4191042139865, 136.9859309938547, 254.5808957860135, 0),
                        new CropKeyFrameModel(258, 113.38461538461539, 214.44732733218075, 134.59680752961773, 265.55267266781925, 0),
                        new CropKeyFrameModel(262, 87.23076923076923, 199.81829148977306, 136.42988159546107, 280.18170851022694, 0),
                        new CropKeyFrameModel(268, 48, 177.87473772616153, 160.97066066781252, 302.1252622738385, 0),
                        new CropKeyFrameModel(277, 0, 177.87473772616153, 178.40359504668163, 302.1252622738385, 0),
                        new CropKeyFrameModel(287, 0, 197.68489042942196, 172.0643461816383, 282.31510957057804, 0)
                    },
                    "Crop 3"
                )
            );
            project.Cropping.CropSegments.Add(
                new CropSegmentModel(100, 322, 2,
                    new KeyFrameModelCollection()
                    {
                        new CropKeyFrameModel(100, 459.42601565411854, 62.09790209790208, 180.57398434588146, 417.9020979020979, 0),
                        new CropKeyFrameModel(120, 436.4462385183365, 62.09790209790208, 203.5537614816635, 417.9020979020979, 0),
                        new CropKeyFrameModel(145, 436.4462385183365, 62.09790209790208, 203.5537614816635, 417.9020979020979, 0),
                        new CropKeyFrameModel(160, 461.8032339785098, 87.45489755807546, 178.1967660214902, 392.54510244192454, 0),
                        new CropKeyFrameModel(184, 461.8032339785098, 87.45489755807546, 178.1967660214902, 392.54510244192454, 0),
                        new CropKeyFrameModel(195, 482.4057927899008, 108.05745636946642, 157.59420721009923, 371.9425436305336, 0),
                        new CropKeyFrameModel(207, 494.29188441185704, 90.62452199059726, 145.70811558814296, 389.37547800940274, 0),
                        new CropKeyFrameModel(215, 494.29188441185704, 77.94602426051057, 145.70811558814296, 402.05397573948943, 0),
                        new CropKeyFrameModel(220, 476.0665439248575, 77.94602426051057, 163.9334560751425, 402.05397573948943, 0),
                        new CropKeyFrameModel(266, 476.0665439248575, 77.94602426051057, 163.9334560751425, 402.05397573948943, 0),
                        new CropKeyFrameModel(282, 443.5778934915103, 66.85233874668472, 196.42210650848972, 413.1476612533153, 0),
                        new CropKeyFrameModel(296, 430.1069896532933, 47.834592151554716, 209.89301034670672, 432.1654078484453, 0),
                        new CropKeyFrameModel(317, 409.50443084190243, 47.834592151554716, 230.49556915809757, 432.1654078484453, 0)
                    },
                    "Crop 4"
                )
            );

            return project;
        }

        public static ProjectModel CreateMockMaskingProject()
        {
            ProjectModel project = new ProjectModel()
            {
                ScriptFileSource = @"TestFiles\AVSSourceTestScript-628x472-23.976fps.avs"
            };
            project.Masking.Shapes.Add(
                new PolygonMaskShapeModel(0, 22, 0,
                    new KeyFrameModelCollection()
                    {
                        new PolygonMaskShapeKeyFrameModel(0,
                            new List<PointD>()
                            {
                                new PointD(394.879161266755, 1.4210854715202004E-14),
                                new PointD(86.63458310016779, 7.105427357601002E-15),
                                new PointD(86.63458310016779, 138.40402909904853)
                            }
                        ),
                        new PolygonMaskShapeKeyFrameModel(10,
                            new List<PointD>()
                            {
                                new PointD(246.96645910005546, 1.0034382205092489E-14),
                                new PointD(86.63458310016779, 7.105427357601002E-15),
                                new PointD(86.63458310016779, 57.05204367593147)
                            }
                        ),
                        new PolygonMaskShapeKeyFrameModel(22,
                            new List<PointD>()
                            {
                                new PointD(394.879161266755, 1.4210854715202004E-14),
                                new PointD(86.63458310016779, 7.105427357601002E-15),
                                new PointD(86.63458310016779, 38.40402909904853)
                            }
                        )
                    },
                    "Polygon 1"
                )
            );
            project.Masking.Shapes.Add(
                new RectangleMaskShapeModel(0, 22, 1,
                    new KeyFrameModelCollection()
                    {
                        new RectangleMaskShapeKeyFrameModel(0, 1.4210854715202004E-14, 0, 115.79574706211527, 472)
                    },
                    "Rectangle 1"
                )
            );
            project.Masking.Shapes.Add(
                new PolygonMaskShapeModel(10, 22, 2,
                    new KeyFrameModelCollection()
                    {
                        new PolygonMaskShapeKeyFrameModel(10,
                            new List<PointD>()
                            {
                                new PointD(490.9001119194184, 196.95116711968294),
                                new PointD(402.4001119194184, 314.9511671196832),
                                new PointD(579.4001119194181, 314.9511671196832)
                            }
                        ),
                        new PolygonMaskShapeKeyFrameModel(22,
                            new List<PointD>()
                            {
                                new PointD(441.77196281557815, 196.95116711968294),
                                new PointD(304.1438137117378, 472.00000614521656),
                                new PointD(579.4001119194181, 472.00000614521656)
                            }
                        )
                    },
                    "Polygon 2"
                )
            );

            return project;
        }
    }
}
