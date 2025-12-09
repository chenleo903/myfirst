import { useEffect } from 'react';
import { Form, Input, Select, DatePicker, Button, Space, message, Upload } from 'antd';
import { UploadOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { useCreateInteraction, useUpdateInteraction } from '../hooks/useInteractions';
import type {
  Interaction,
  CreateInteractionRequest,
  InteractionChannel,
  CustomerStatus,
} from '../types';

const { Option } = Select;
const { TextArea } = Input;

// Channel options
const channelOptions: { value: InteractionChannel; label: string }[] = [
  { value: 'Phone', label: '电话' },
  { value: 'Wechat', label: '微信' },
  { value: 'Email', label: '邮件' },
  { value: 'Offline', label: '线下' },
  { value: 'Other', label: '其他' },
];

// Stage options (customer status at time of interaction)
const stageOptions: { value: CustomerStatus; label: string }[] = [
  { value: 'Lead', label: '线索' },
  { value: 'Contacted', label: '已联系' },
  { value: 'NeedsAnalyzed', label: '待分析' },
  { value: 'Quoted', label: '已报价' },
  { value: 'Negotiating', label: '谈判中' },
  { value: 'Won', label: '成交' },
  { value: 'Lost', label: '流失' },
];

interface InteractionFormProps {
  customerId: string;
  interaction?: Interaction | null;
  onSuccess?: () => void;
  onCancel?: () => void;
}

interface FormValues {
  happenedAt: dayjs.Dayjs;
  channel: InteractionChannel;
  stage?: CustomerStatus;
  title: string;
  summary?: string;
  rawContent?: string;
  nextAction?: string;
}


export default function InteractionForm({
  customerId,
  interaction,
  onSuccess,
  onCancel,
}: InteractionFormProps) {
  const [form] = Form.useForm<FormValues>();
  const isEditing = !!interaction;

  const createInteraction = useCreateInteraction();
  const updateInteraction = useUpdateInteraction();

  // Populate form when editing
  useEffect(() => {
    if (interaction) {
      form.setFieldsValue({
        happenedAt: dayjs(interaction.happenedAt),
        channel: interaction.channel,
        stage: interaction.stage,
        title: interaction.title,
        summary: interaction.summary,
        rawContent: interaction.rawContent,
        nextAction: interaction.nextAction,
      });
    } else {
      form.resetFields();
      form.setFieldsValue({
        happenedAt: dayjs(),
      });
    }
  }, [interaction, form]);

  const handleSubmit = async (values: FormValues) => {
    const request: CreateInteractionRequest = {
      happenedAt: values.happenedAt.toISOString(),
      channel: values.channel,
      stage: values.stage,
      title: values.title,
      summary: values.summary,
      rawContent: values.rawContent,
      nextAction: values.nextAction,
      // Attachments placeholder - would need file upload implementation
      attachments: interaction?.attachments,
    };

    try {
      if (isEditing && interaction) {
        await updateInteraction.mutateAsync({
          id: interaction.id,
          customerId,
          request,
        });
        message.success('互动记录已更新');
      } else {
        await createInteraction.mutateAsync({
          customerId,
          request,
        });
        message.success('互动记录已创建');
      }
      onSuccess?.();
    } catch (error) {
      const err = error as { response?: { data?: { errors?: { message: string }[] } } };
      const errorMessage = err.response?.data?.errors?.[0]?.message || '操作失败';
      message.error(errorMessage);
    }
  };

  const isSubmitting = createInteraction.isPending || updateInteraction.isPending;


  return (
    <Form
      form={form}
      layout="vertical"
      onFinish={handleSubmit}
      initialValues={{
        happenedAt: dayjs(),
      }}
    >
      <Form.Item
        name="happenedAt"
        label="发生时间"
        rules={[{ required: true, message: '请选择发生时间' }]}
      >
        <DatePicker
          showTime
          format="YYYY-MM-DD HH:mm"
          style={{ width: '100%' }}
          placeholder="请选择发生时间"
        />
      </Form.Item>

      <Form.Item
        name="channel"
        label="渠道"
        rules={[{ required: true, message: '请选择渠道' }]}
      >
        <Select placeholder="请选择渠道">
          {channelOptions.map((opt) => (
            <Option key={opt.value} value={opt.value}>
              {opt.label}
            </Option>
          ))}
        </Select>
      </Form.Item>

      <Form.Item name="stage" label="客户阶段">
        <Select placeholder="请选择客户当时所处阶段" allowClear>
          {stageOptions.map((opt) => (
            <Option key={opt.value} value={opt.value}>
              {opt.label}
            </Option>
          ))}
        </Select>
      </Form.Item>

      <Form.Item
        name="title"
        label="标题"
        rules={[
          { required: true, message: '请输入标题' },
          { max: 200, message: '标题不能超过200个字符' },
        ]}
      >
        <Input placeholder="请输入互动标题" />
      </Form.Item>

      <Form.Item
        name="summary"
        label="摘要"
        rules={[{ max: 2000, message: '摘要不能超过2000个字符' }]}
      >
        <TextArea rows={3} placeholder="请输入互动摘要" />
      </Form.Item>

      <Form.Item
        name="rawContent"
        label="原始内容"
        rules={[{ max: 10000, message: '原始内容不能超过10000个字符' }]}
      >
        <TextArea rows={4} placeholder="请输入原始沟通内容" />
      </Form.Item>

      <Form.Item
        name="nextAction"
        label="下一步行动"
        rules={[{ max: 500, message: '下一步行动不能超过500个字符' }]}
      >
        <Input placeholder="请输入下一步行动计划" />
      </Form.Item>

      {/* Attachment upload placeholder */}
      <Form.Item label="附件" tooltip="附件上传功能开发中">
        <Upload disabled>
          <Button icon={<UploadOutlined />} disabled>
            上传附件（开发中）
          </Button>
        </Upload>
        {interaction?.attachments && interaction.attachments.length > 0 && (
          <div style={{ marginTop: 8 }}>
            <span>已有附件：</span>
            {interaction.attachments.map((att, idx) => (
              <a
                key={idx}
                href={att.url}
                target="_blank"
                rel="noopener noreferrer"
                style={{ marginLeft: 8 }}
              >
                {att.fileName || `附件${idx + 1}`}
              </a>
            ))}
          </div>
        )}
      </Form.Item>

      <Form.Item>
        <Space>
          <Button type="primary" htmlType="submit" loading={isSubmitting}>
            {isEditing ? '保存' : '创建'}
          </Button>
          <Button onClick={onCancel}>取消</Button>
        </Space>
      </Form.Item>
    </Form>
  );
}
